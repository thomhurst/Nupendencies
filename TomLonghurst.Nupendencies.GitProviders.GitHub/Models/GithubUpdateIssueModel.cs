using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubUpdateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; init; }
    
    [JsonPropertyName("issue_number"), JsonProperty("issue_number")]
    public int IssueNumber { get; init; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public string State { get; init; }
}