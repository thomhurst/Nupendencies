using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsTeam(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("identityUrl")] string IdentityUrl,
    [property: JsonPropertyName("projectName")] string ProjectName,
    [property: JsonPropertyName("projectId")] string ProjectId
);