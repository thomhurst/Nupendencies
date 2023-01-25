using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsGitRepositoryResponse(
        [property: JsonPropertyName("value")] IReadOnlyList<DevOpsGitRepository> Repositories,
        [property: JsonPropertyName("count")] int Count
    );