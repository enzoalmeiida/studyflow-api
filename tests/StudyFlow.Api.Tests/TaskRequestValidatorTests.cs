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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTitleIsMissingOrWhitespace_ReturnsTitleError(string? title)
    {
        Assert.Contains("title", TaskRequestValidator.Validate(
            new CreateTaskRequest(title, null, null)).Keys);
    }

    [Theory]
    [InlineData(120, false)]
    [InlineData(121, true)]
    public void Validate_TitleLengthBoundary_IsEnforced(int length, bool invalid)
    {
        var errors = TaskRequestValidator.Validate(
            new CreateTaskRequest(new string('a', length), null, null));

        Assert.Equal(invalid, errors.ContainsKey("title"));
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(501, true)]
    public void Validate_DescriptionLengthBoundary_IsEnforced(int length, bool invalid)
    {
        var errors = TaskRequestValidator.Validate(
            new CreateTaskRequest("Estudar", new string('a', length), null));

        Assert.Equal(invalid, errors.ContainsKey("description"));
    }

    [Fact]
    public void Validate_WhenTitleHasSurroundingSpaces_UsesTrimmedLength()
    {
        var errors = TaskRequestValidator.Validate(
            new CreateTaskRequest("  " + new string('a', 120) + "  ", null, null));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenOptionalFieldsAreNull_AcceptsDefaultPriority()
    {
        var request = new CreateTaskRequest("Estudar", null, null);

        Assert.Empty(TaskRequestValidator.Validate(request));
        Assert.Equal(TaskPriority.Medium, request.Priority);
    }
}
