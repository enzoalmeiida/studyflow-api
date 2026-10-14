using StudyFlow.Api.Contracts;

namespace StudyFlow.Api.Validation;

public static class TaskRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateTaskRequest request) =>
        ValidateFields(request.Title, request.Description, request.DueDate);

    public static Dictionary<string, string[]> Validate(UpdateTaskRequest request) =>
        ValidateFields(request.Title, request.Description, request.DueDate);

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
