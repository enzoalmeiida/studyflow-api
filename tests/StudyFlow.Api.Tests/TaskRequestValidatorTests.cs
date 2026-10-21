using StudyFlow.Api.Contracts;
using StudyFlow.Api.Models;
using StudyFlow.Api.Validation;

namespace StudyFlow.Api.Tests;

public sealed class TaskRequestValidatorTests
{
    [Fact]
    public void Validate_WhenTitleIsEmpty_ReturnsTitleError()
    {
        var request = new CreateTaskRequest(" ", null, null);

        var errors = TaskRequestValidator.Validate(request);

        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void Validate_WhenFieldsAreValid_ReturnsNoErrors()
    {
        var request = new UpdateTaskRequest(
            "Estudar Minimal APIs",
            "Revisar o conteúdo da disciplina",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            TaskPriority.High,
            StudyTaskStatus.InProgress);

        var errors = TaskRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenDueDateIsInThePast_ReturnsDueDateError()
    {
        var request = new CreateTaskRequest(
            "Entregar atividade",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var errors = TaskRequestValidator.Validate(request);

        Assert.Contains("dueDate", errors.Keys);
    }
    [Fact]
    public void Validate_WhenCreatePriorityIsUndefined_ReturnsPriorityError()
    {
        var request = new CreateTaskRequest("Estudar", null, null, (TaskPriority)99);

        Assert.Contains("priority", TaskRequestValidator.Validate(request).Keys);
    }

    [Fact]
    public void Validate_WhenUpdateEnumsAreUndefined_ReturnsBothErrors()
    {
        var request = new UpdateTaskRequest(
            "Estudar", null, null, (TaskPriority)99, (StudyTaskStatus)99);

        var errors = TaskRequestValidator.Validate(request);

        Assert.Contains("priority", errors.Keys);
        Assert.Contains("status", errors.Keys);
    }
}
