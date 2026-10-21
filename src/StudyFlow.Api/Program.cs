using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StudyFlow.Api.Data;
using StudyFlow.Api.Endpoints;
using StudyFlow.Api.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "StudyFlow API";
        document.Info.Version = "v1";
        document.Info.Description = "API RESTful de organização de tarefas acadêmicas — CP6 de C# Software Development.";
        return Task.CompletedTask;
    }));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Encaminha falhas de leitura da requisição ao tratamento padronizado em qualquer ambiente.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A conexão 'DefaultConnection' não foi configurada.");

builder.Services.AddDbContext<StudyFlowDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("StudyFlow API");
    options.WithTheme(ScalarTheme.Moon);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

app.MapTaskEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
