using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubCreateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public required string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public required string Repo { get; init; }
    
    [JsonPropertyName("title"), JsonProperty("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("body"), JsonProperty("body")]
    public required string Body { get; init; }
}