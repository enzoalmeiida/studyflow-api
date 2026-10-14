using Microsoft.EntityFrameworkCore;
using StudyFlow.Api.Contracts;
using StudyFlow.Api.Data;
using StudyFlow.Api.Models;
using StudyFlow.Api.Validation;

namespace StudyFlow.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks")
            .WithTags("Tasks");

        group.MapGet("/", GetAllAsync)
            .WithName("GetTasks")
            .WithSummary("Lista tarefas, com filtro opcional por status.")
            .Produces<List<TaskResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetByIdAsync)
            .WithName("GetTaskById")
            .WithSummary("Busca uma tarefa por identificador.")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTask")
            .WithSummary("Cria uma nova tarefa.")
            .Accepts<CreateTaskRequest>("application/json")
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", UpdateAsync)
            .WithName("UpdateTask")
            .WithSummary("Atualiza todos os dados editáveis de uma tarefa.")
            .Accepts<UpdateTaskRequest>("application/json")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteAsync)
            .WithName("DeleteTask")
            .WithSummary("Exclui uma tarefa.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> GetAllAsync(
        StudyTaskStatus? status,
        StudyFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        var tasks = await query
            .OrderBy(task => task.Status)
            .ThenBy(task => task.DueDate)
            .Select(task => TaskResponse.FromEntity(task))
            .ToListAsync(cancellationToken);

        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        StudyFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return task is null
            ? Results.NotFound(CreateNotFoundProblem(id))
            : Results.Ok(TaskResponse.FromEntity(task));
    }

    private static async Task<IResult> CreateAsync(
        CreateTaskRequest request,
        StudyFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = TaskRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var task = new StudyTask
        {
            Title = request.Title!.Trim(),
            Description = NormalizeOptionalText(request.Description),
            DueDate = request.DueDate,
            Priority = request.Priority,
            Status = StudyTaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.CreatedAtRoute(
            "GetTaskById",
            new { id = task.Id },
            TaskResponse.FromEntity(task));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateTaskRequest request,
        StudyFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = TaskRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (task is null)
        {
            return Results.NotFound(CreateNotFoundProblem(id));
        }

        task.Title = request.Title!.Trim();
        task.Description = NormalizeOptionalText(request.Description);
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(TaskResponse.FromEntity(task));
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        StudyFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.Tasks
            .Where(task => task.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows == 0
            ? Results.NotFound(CreateNotFoundProblem(id))
            : Results.NoContent();
    }

    private static object CreateNotFoundProblem(int id) => new
    {
        title = "Tarefa não encontrada",
        status = StatusCodes.Status404NotFound,
        detail = $"Não existe uma tarefa com o identificador {id}."
    };

    private static string? NormalizeOptionalText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
