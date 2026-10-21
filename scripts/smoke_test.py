"""Testes HTTP com a API compilada e SQLite temporário (Python 3.11+)."""
import datetime
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
BASE = "http://127.0.0.1:5139"
DLL = ROOT / "src/StudyFlow.Api/bin/Release/net10.0/StudyFlow.Api.dll"
PASSED = []


def request(method, route, expected, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(
        BASE + route, data=data, method=method,
        headers={"Content-Type": "application/json"})
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read().decode("utf-8")
        assert response.status == expected, (
            f"{method} {route}: esperado {expected}, recebido {response.status}: {raw}")
        payload = json.loads(raw) if raw and "json" in response.headers.get("Content-Type", "") else raw
        headers = dict(response.headers)
    PASSED.append(f"{method} {route} => {expected}")
    print(PASSED[-1], flush=True)
    return payload, headers


def start(env, log):
    process = subprocess.Popen(
        ["dotnet", str(DLL), "--urls", BASE],
        cwd=ROOT / "src/StudyFlow.Api", env=env,
        stdout=log, stderr=subprocess.STDOUT)
    try:
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            if process.poll() is not None:
                raise RuntimeError("A API encerrou antes de iniciar. Consulte artifacts/api-smoke.log.")
            try:
                with urllib.request.urlopen(BASE + "/openapi/v1.json", timeout=1) as response:
                    if response.status == 200:
                        return process
            except (OSError, urllib.error.URLError):
                time.sleep(0.25)
        raise TimeoutError("A API não iniciou em 45 segundos.")
    except BaseException:
        stop(process)
        raise


def stop(process):
    if process is not None and process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)


def main():
    if not DLL.exists():
        raise SystemExit("Execute primeiro: dotnet build --configuration Release")
    artifacts = ROOT / "artifacts"
    artifacts.mkdir(exist_ok=True)
    process = None
    with tempfile.TemporaryDirectory(prefix="studyflow-test-") as folder:
        env = dict(os.environ)
        env["ConnectionStrings__DefaultConnection"] = f"Data Source={Path(folder) / 'test.db'}"
        env["ASPNETCORE_ENVIRONMENT"] = "Development"
        with (artifacts / "api-smoke.log").open("w", encoding="utf-8") as log:
            try:
                process = start(env, log)
                spec, _ = request("GET", "/openapi/v1.json", 200)
                operations = {
                    (route.rstrip("/"), method)
                    for route, item in spec["paths"].items()
                    for method in item
                }
                assert {
                    ("/api/tasks", "get"), ("/api/tasks", "post"),
                    ("/api/tasks/{id}", "get"), ("/api/tasks/{id}", "put"),
                    ("/api/tasks/{id}", "delete"),
                } <= operations, "Faltam operações no OpenAPI."
                html, _ = request("GET", "/scalar/v1", 200)
                assert "scalar" in html.lower()
                tasks, _ = request("GET", "/api/tasks", 200)
                assert tasks == []

                due = (datetime.datetime.now(datetime.timezone.utc).date()
                       + datetime.timedelta(days=3)).isoformat()
                body = {
                    "title": "Estudar Minimal APIs", "description": "Teste HTTP com SQLite",
                    "dueDate": due, "priority": "High",
                }
                created, headers = request("POST", "/api/tasks", 201, body)
                task_id = created["id"]
                assert created["status"] == "Pending" and created["priority"] == "High"
                location = next(value for key, value in headers.items() if key.lower() == "location")
                assert location.endswith(f"/api/tasks/{task_id}")
                tasks, _ = request("GET", "/api/tasks", 200)
                assert any(task["id"] == task_id for task in tasks)
                found, _ = request("GET", f"/api/tasks/{task_id}", 200)
                assert found == created
                filtered, _ = request("GET", "/api/tasks?status=Pending", 200)
                assert len(filtered) == 1 and filtered[0]["id"] == task_id
                filtered, _ = request("GET", "/api/tasks?status=Completed", 200)
                assert filtered == []

                updated_body = {**body, "title": "Conteúdo revisado", "status": "Completed"}
                updated, _ = request("PUT", f"/api/tasks/{task_id}", 200, updated_body)
                assert updated["title"] == "Conteúdo revisado" and updated["status"] == "Completed"

                problem, _ = request("POST", "/api/tasks", 400, {**body, "title": " "})
                assert "title" in problem["errors"]
                problem, _ = request("POST", "/api/tasks", 400, {**body, "priority": 99})
                assert "priority" in problem["errors"]
                problem, _ = request("PUT", f"/api/tasks/{task_id}", 400,
                                     {**updated_body, "status": 99})
                assert "status" in problem["errors"]
                request("GET", "/api/tasks?status=99", 400)
                request("GET", "/api/tasks/2147483647", 404)
                request("PUT", "/api/tasks/2147483647", 404, updated_body)
                request("DELETE", "/api/tasks/2147483647", 404)

                # Mesmo arquivo SQLite em outro processo: comprova persistência real.
                stop(process)
                process = start(env, log)
                persisted, _ = request("GET", f"/api/tasks/{task_id}", 200)
                assert persisted["title"] == "Conteúdo revisado"
                assert persisted["status"] == "Completed"

                request("DELETE", f"/api/tasks/{task_id}", 204)
                problem, headers = request("GET", f"/api/tasks/{task_id}", 404)
                assert problem["status"] == 404
                assert any(key.lower() == "content-type" and "application/problem+json" in value
                           for key, value in headers.items())
                request("DELETE", f"/api/tasks/{task_id}", 404)
                tasks, _ = request("GET", "/api/tasks", 200)
                assert tasks == []
                (artifacts / "smoke-results.json").write_text(
                    json.dumps({"passed": len(PASSED), "requests": PASSED},
                               indent=2, ensure_ascii=False), encoding="utf-8")
                print(f"OK: {len(PASSED)} requisições HTTP e persistência verificadas.", flush=True)
            finally:
                stop(process)


if __name__ == "__main__":
    main()
