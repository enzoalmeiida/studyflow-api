# StudyFlow API

**Disciplina:** C# Software Development  
**Avaliação:** CP6 — API RESTful com Minimal API e Entity Framework Core

## Integrantes

| Nome | RM |
|---|---|
| Enzo Almeida | RM556900 |
| Gabriel de Mello | RM554421 |
| Guilherme Moreira | RM557290 |

## Apresentação do projeto

O StudyFlow é uma API RESTful para organização de tarefas acadêmicas. A aplicação permite cadastrar, consultar, atualizar e excluir atividades de estudo, como trabalhos, leituras, provas e exercícios.

O projeto atende estudantes que precisam centralizar seus compromissos acadêmicos e acompanhar prazos, prioridades e andamento das atividades. Cada tarefa possui título, descrição, prazo opcional, prioridade, status e datas de criação e atualização.

## Tecnologias e arquitetura

- C# e .NET 10;
- ASP.NET Core Minimal APIs;
- Entity Framework Core 10;
- SQLite;
- OpenAPI e Scalar;
- xUnit para testes unitários;
- GitHub Actions para integração contínua.

Os endpoints são registrados com `MapGet`, `MapPost`, `MapPut` e `MapDelete`, sem Controllers ou uso do atributo `[ApiController]`. O código está organizado em entidades, contratos de entrada e saída, contexto de dados, endpoints, validações e tratamento de exceções.

## Banco de dados

A persistência utiliza **SQLite**, com mapeamento das entidades pelo `StudyFlowDbContext`. As migrations e o snapshot do modelo estão versionados em `src/StudyFlow.Api/Migrations`.

O arquivo `studyflow.db` é criado localmente durante a aplicação das migrations. Os comandos de migration e inicialização são executados na pasta `src/StudyFlow.Api`, de forma que ambos utilizem o mesmo arquivo local. Arquivos de banco de dados não são versionados no repositório.

## Execução local

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0);
- Git;
- Acesso ao NuGet para restauração das dependências.

### Clonagem, restauração e aplicação das migrations

```bash
git clone https://github.com/enzoalmeiida/studyflow-api.git
cd studyflow-api
dotnet tool restore
dotnet restore
cd src/StudyFlow.Api
dotnet ef database update
```

### Inicialização da API

Na mesma pasta `src/StudyFlow.Api`:

```bash
dotnet run
```

A inicialização também aplica migrations pendentes por meio de `MigrateAsync`. O perfil HTTP de `launchSettings.json` configura a porta `5130`.

| Recurso | Endereço local |
|---|---|
| Documentação interativa e testes no Scalar | [http://localhost:5130/scalar/v1](http://localhost:5130/scalar/v1) |
| Documento OpenAPI | [http://localhost:5130/openapi/v1.json](http://localhost:5130/openapi/v1.json) |

## Endpoints

| Método | Rota | Operação | Respostas principais |
|---|---|---|---|
| `GET` | `/api/tasks` | Listagem de tarefas | `200 OK` |
| `GET` | `/api/tasks?status=Pending` | Listagem com filtro por status | `200 OK`, `400 Bad Request` |
| `GET` | `/api/tasks/{id}` | Consulta de tarefa por ID | `200 OK`, `404 Not Found` |
| `POST` | `/api/tasks` | Cadastro de tarefa | `201 Created`, `400 Bad Request` |
| `PUT` | `/api/tasks/{id}` | Atualização dos dados editáveis | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/tasks/{id}` | Exclusão de tarefa | `204 No Content`, `404 Not Found` |

O cadastro retorna o recurso criado e o cabeçalho `Location` com o endereço da consulta por ID. A exclusão bem-sucedida retorna `204 No Content`, sem corpo de resposta.

### Prioridades e status

| Campo | Valores |
|---|---|
| `priority` | `Low`, `Medium`, `High` |
| `status` | `Pending`, `InProgress`, `Completed` |

Novas tarefas recebem o status `Pending`. O status pode ser alterado pelo endpoint de atualização. O PUT exige `title`, `priority` e `status`; `description` e `dueDate` podem ser omitidos ou enviados como `null`. A omissão de prioridade no POST utiliza `Medium`.

### Corpo da requisição de cadastro

```json
{
  "title": "Estudar Minimal APIs",
  "description": "Revisar endpoints e programação assíncrona",
  "dueDate": null,
  "priority": "High"
}
```

### Corpo da requisição de atualização

```json
{
  "title": "Estudar Minimal APIs e EF Core",
  "description": "Conteúdo revisado e exercício concluído",
  "dueDate": null,
  "priority": "High",
  "status": "Completed"
}
```

O campo `dueDate` aceita uma data no formato `AAAA-MM-DD` ou `null` quando não há prazo definido.

O arquivo [StudyFlow.Api.http](src/StudyFlow.Api/StudyFlow.Api.http) contém requisições para os endpoints, compatíveis com o Visual Studio e com a extensão REST Client do VS Code.

## Programação assíncrona

Todas as operações de acesso e persistência de dados utilizam os métodos assíncronos do EF Core com `async` e `await`.

| Operação | Métodos utilizados |
|---|---|
| Consultas | `ToListAsync`, `FirstOrDefaultAsync` |
| Inclusão | `AddAsync`, `SaveChangesAsync` |
| Atualização | `SaveChangesAsync` |
| Exclusão | `ExecuteDeleteAsync` |
| Aplicação das migrations na inicialização | `MigrateAsync` |

Os endpoints recebem `CancellationToken` e o repassam às operações do banco. As consultas de leitura utilizam `AsNoTracking`.

## Validações e tratamento de erros

- Título obrigatório, com até 120 caracteres.
- Descrição opcional, com até 500 caracteres.
- Prazo opcional, sem aceitação de datas passadas, considerando a data UTC.
- Validação dos valores de prioridade, status e filtro de status.
- Respostas `400 Bad Request` para falhas de validação, JSON malformado, campos obrigatórios ausentes e tipos inválidos.
- Respostas `415 Unsupported Media Type` para corpos enviados com tipo de conteúdo incompatível.
- Respostas `405 Method Not Allowed` para métodos não aceitos em uma rota.
- Respostas `404 Not Found` para identificadores inexistentes.
- Respostas de erro em formato `ProblemDetails`, com o tipo de conteúdo `application/problem+json`.
- Tratamento consistente de requisições inválidas em Development e Production.
- Tratamento global de exceções, com registro em log e resposta `500 Internal Server Error` sem exposição de detalhes internos.

## Estrutura do repositório

```text
studyflow-api/
├── .config/dotnet-tools.json
├── .github/workflows/ci.yml
├── docs/
│   ├── evidencias/
│   └── postman/StudyFlow.postman_collection.json
├── scripts/smoke_test.py
├── src/StudyFlow.Api/
│   ├── Contracts/
│   ├── Data/
│   ├── Endpoints/
│   ├── Infrastructure/
│   ├── Migrations/
│   ├── Models/
│   ├── Validation/
│   └── Program.cs
├── tests/StudyFlow.Api.Tests/
├── global.json
├── README.md
└── StudyFlow.slnx
```

## Testes e verificação técnica

Os comandos de teste são executados na raiz do repositório (`studyflow-api`).

### Testes unitários

```bash
dotnet test
```

Os testes xUnit abrangem título obrigatório, limites de tamanho, campos opcionais, prazo passado, valores inválidos de prioridade e status e obrigatoriedade dos campos do contrato JSON de atualização.

### Testes de integração HTTP

**Requisito adicional:** Python 3.11 ou superior.

```bash
dotnet build --configuration Release
python scripts/smoke_test.py --environment Production
python scripts/smoke_test.py --environment Development
```

O script inicia a API com um banco SQLite temporário e verifica a documentação OpenAPI, o Scalar, o CRUD completo, a filtragem por status, a normalização dos textos, os valores padrão, os erros 400/404/405/415 e a persistência após reiniciar o processo. Também confirma que requisições inválidas não alteram o recurso.

### Resultado da verificação

A verificação automatizada abrange:

- Compilação com avisos tratados como erros.
- Testes unitários de validação e contrato JSON.
- Testes HTTP com SQLite real em Production e Development.
- Aplicação das migrations e verificação de consistência do snapshot.
- Persistência dos dados confirmada após reinicialização da API.

Os resultados e logs ficam disponíveis nas execuções do GitHub Actions.

O workflow [CI](https://github.com/enzoalmeiida/studyflow-api/actions/workflows/ci.yml) executa essas verificações a cada push e pull request.

### Coleção do Postman

A coleção [StudyFlow.postman_collection.json](docs/postman/StudyFlow.postman_collection.json) contém os cinco endpoints, o filtro por status e cenários de erro, com verificações de resposta. A variável `baseUrl` utiliza `http://localhost:5130`. O cadastro captura o identificador da tarefa na variável `taskId`, utilizada pelas requisições seguintes.

Com a API em execução, a coleção pode ser importada no Postman e executada sequencialmente pelo Collection Runner.

## Atendimento aos requisitos da atividade

| Critério | Implementação e evidência |
|---|---|
| REST com Minimal API | [Mapeamento dos métodos GET, POST, PUT e DELETE](src/StudyFlow.Api/Endpoints/TaskEndpoints.cs), códigos HTTP e cabeçalho Location |
| Entity Framework Core | [DbContext e mapeamento](src/StudyFlow.Api/Data/StudyFlowDbContext.cs), entidade e [migrations versionadas](src/StudyFlow.Api/Migrations) |
| Boas práticas | DTOs, [validações](src/StudyFlow.Api/Validation/TaskRequestValidator.cs), consultas sem rastreamento e [tratamento de exceções](src/StudyFlow.Api/Infrastructure/GlobalExceptionHandler.cs) |
| Documentação e evidências | Contexto, integrantes, execução local, OpenAPI, Scalar, coleção do Postman e capturas dos endpoints |
| Programação assíncrona | Consultas, inclusão, persistência, exclusão e migrations com métodos assíncronos e `CancellationToken` nos endpoints |

## Evidências de funcionamento

As capturas de tela estão versionadas em [docs/evidencias](docs/evidencias).

### Documentação OpenAPI no Scalar

![Documentação dos endpoints no Scalar](docs/evidencias/01-scalar-documentacao.png)

### POST — cadastro de tarefa

Resposta registrada: `201 Created`.

![Cadastro de tarefa com resposta HTTP 201](docs/evidencias/02-post-criar-tarefa.png)

### GET — listagem de tarefas

Resposta registrada: `200 OK`.

![Listagem de tarefas com resposta HTTP 200](docs/evidencias/03-get-listar-tarefas.png)

### GET — consulta de tarefa por ID

Resposta registrada: `200 OK`.

![Consulta de tarefa por ID com resposta HTTP 200](docs/evidencias/04-get-buscar-tarefa.png)

### PUT — atualização de tarefa

Resposta registrada: `200 OK`.

![Atualização de tarefa com resposta HTTP 200](docs/evidencias/05-put-atualizar-tarefa.png)

### DELETE — exclusão de tarefa

Resposta registrada: `204 No Content`.

![Exclusão de tarefa com resposta HTTP 204](docs/evidencias/06-delete-excluir-tarefa.png)
