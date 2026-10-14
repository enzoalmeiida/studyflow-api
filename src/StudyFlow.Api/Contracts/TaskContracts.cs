using StudyFlow.Api.Models;

namespace StudyFlow.Api.Contracts;

public sealed record CreateTaskRequest(
    string? Title,
    string? Description,
    DateOnly? DueDate,
    TaskPriority Priority = TaskPriority.Medium);

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    DateOnly? DueDate,
    TaskPriority Priority,
    StudyTaskStatus Status);

public sealed record TaskResponse(
    int Id,
    string Title,
    string? Description,
    DateOnly? DueDate,
    TaskPriority Priority,
    StudyTaskStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TaskResponse FromEntity(StudyTask task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.DueDate,
        task.Priority,
        task.Status,
        task.CreatedAt,
        task.UpdatedAt);
}
