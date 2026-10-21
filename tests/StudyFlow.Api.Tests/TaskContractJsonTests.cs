using System.Text.Json;
using System.Text.Json.Serialization;
using StudyFlow.Api.Contracts;
using StudyFlow.Api.Models;

namespace StudyFlow.Api.Tests;

public sealed class TaskContractJsonTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData("""{"title":"Estudar"}""")]
    [InlineData("""{"title":"Estudar","priority":"High"}""")]
    [InlineData("""{"title":"Estudar","status":"Completed"}""")]
    public void DeserializeUpdate_WhenAnEnumIsMissing_RejectsIncompleteRequest(string json)
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<UpdateTaskRequest>(json, Options));
    }

    [Fact]
    public void DeserializeUpdate_WhenRequiredEnumsArePresent_AcceptsRequest()
    {
        var request = JsonSerializer.Deserialize<UpdateTaskRequest>(
            """{"title":"Estudar","priority":"High","status":"Completed"}""", Options);

        Assert.NotNull(request);
        Assert.Equal(TaskPriority.High, request.Priority);
        Assert.Equal(StudyTaskStatus.Completed, request.Status);
    }

    [Fact]
    public void DeserializeCreate_WhenPriorityIsOmitted_UsesMedium()
    {
        var request = JsonSerializer.Deserialize<CreateTaskRequest>(
            """{"title":"Estudar"}""", Options);

        Assert.NotNull(request);
        Assert.Equal(TaskPriority.Medium, request.Priority);
    }
}
