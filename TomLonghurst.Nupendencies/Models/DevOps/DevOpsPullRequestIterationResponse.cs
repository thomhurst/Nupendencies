using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsPullRequestIterationResponse(
    [property: JsonPropertyName("value")] IReadOnlyList<DevOpsPullRequestIteration> Value,
    [property: JsonPropertyName("count")] int Count
);