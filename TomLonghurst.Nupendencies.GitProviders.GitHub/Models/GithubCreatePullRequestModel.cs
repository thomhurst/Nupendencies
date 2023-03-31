using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubCreatePullRequestModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; init; }
    
    [JsonPropertyName("title"), JsonProperty("title")]
    public string Title { get; init; }

    [JsonPropertyName("body"), JsonProperty("body")]
    public string Body { get; init; }
    
    [JsonPropertyName("head"), JsonProperty("head")]
    public string Head { get; init; }
    
    [JsonPropertyName("base"), JsonProperty("base")]
    public string Base { get; init; }
}