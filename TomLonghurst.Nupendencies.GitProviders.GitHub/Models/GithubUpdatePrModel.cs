using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Models;

public class GitHubUpdatePrModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public required string Owner { get; init; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public required string Repo { get; init; }
    
    [JsonPropertyName("pull_number"), JsonProperty("pull_number")]
    public required int PrNumber { get; init; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public required string State { get; init; }
}