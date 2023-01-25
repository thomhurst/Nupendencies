using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsPullRequestThreadPropertyValue(
    [property: JsonPropertyName("$value")] string? Value
);