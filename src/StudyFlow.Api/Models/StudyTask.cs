namespace StudyFlow.Api.Models;

public sealed class StudyTask
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public StudyTaskStatus Status { get; set; } = StudyTaskStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum StudyTaskStatus
{
    Pending,
    InProgress,
    Completed
}
