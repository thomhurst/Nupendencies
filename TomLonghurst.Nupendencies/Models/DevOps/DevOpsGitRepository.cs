using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsGitRepository(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("remoteUrl")] string RemoteUrl,
    [property: JsonPropertyName("project")] Project Project,
    [property: JsonPropertyName("isDisabled")] bool IsDisabled,
    [property: JsonPropertyName("defaultBranch")] string DefaultBranch
);