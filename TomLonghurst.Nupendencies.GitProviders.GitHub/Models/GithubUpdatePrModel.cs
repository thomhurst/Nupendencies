using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubUpdatePrModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; init; }
    
    [JsonPropertyName("pull_number"), JsonProperty("pull_number")]
    public int PrNumber { get; init; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public string State { get; init; }
}