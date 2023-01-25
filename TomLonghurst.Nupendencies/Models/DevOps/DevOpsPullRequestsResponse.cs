using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsPullRequestsResponse(
        [property: JsonPropertyName("value")] IReadOnlyList<DevOpsPullRequest> PullRequests,
        [property: JsonPropertyName("count")] int Count
    );