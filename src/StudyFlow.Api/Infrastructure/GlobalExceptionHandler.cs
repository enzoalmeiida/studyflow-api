using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StudyFlow.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var badRequest = exception as BadHttpRequestException;
        var statusCode = badRequest?.StatusCode ?? StatusCodes.Status500InternalServerError;

        if (badRequest is not null)
        {
            logger.LogWarning("Requisição inválida: {Method} {Path}, HTTP {StatusCode}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode);
        }
        else
        {
            logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        var (title, detail) = statusCode switch
        {
            StatusCodes.Status400BadRequest => (
                "Requisição inválida",
                "Verifique o JSON, os campos obrigatórios e os tipos dos parâmetros informados."),
            StatusCodes.Status415UnsupportedMediaType => (
                "Tipo de conteúdo não suportado",
                "O corpo da requisição deve utilizar Content-Type: application/json."),
            _ => (
                "Erro interno do servidor",
                "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }
}
