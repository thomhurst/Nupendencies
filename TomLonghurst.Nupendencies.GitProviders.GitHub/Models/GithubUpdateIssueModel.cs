using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubUpdateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public required string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public required string Repo { get; init; }
    
    [JsonPropertyName("issue_number"), JsonProperty("issue_number")]
    public required int IssueNumber { get; init; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public required string State { get; init; }
}