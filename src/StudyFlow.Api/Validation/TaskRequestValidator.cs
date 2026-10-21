using StudyFlow.Api.Contracts;
using StudyFlow.Api.Models;

namespace StudyFlow.Api.Validation;

public static class TaskRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateTaskRequest request)
    {
        var errors = ValidateFields(request.Title, request.Description, request.DueDate);
        ValidatePriority(request.Priority, errors);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(UpdateTaskRequest request)
    {
        var errors = ValidateFields(request.Title, request.Description, request.DueDate);
        ValidatePriority(request.Priority, errors);

        if (!Enum.IsDefined(request.Status))
        {
            errors["status"] = ["O status deve ser Pending, InProgress ou Completed."];
        }

        return errors;
    }

    private static void ValidatePriority(
        TaskPriority priority,
        Dictionary<string, string[]> errors)
    {
        if (!Enum.IsDefined(priority))
        {
            errors["priority"] = ["A prioridade deve ser Low, Medium ou High."];
        }
    }

    private static Dictionary<string, string[]> ValidateFields(
        string? title,
        string? description,
        DateOnly? dueDate)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["O título é obrigatório."];
        }
        else if (title.Trim().Length > 120)
        {
            errors["title"] = ["O título deve ter no máximo 120 caracteres."];
        }

        if (description?.Length > 500)
        {
            errors["description"] = ["A descrição deve ter no máximo 500 caracteres."];
        }

        if (dueDate is not null && dueDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors["dueDate"] = ["O prazo não pode ser uma data passada."];
        }

        return errors;
    }
}
