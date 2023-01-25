using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Models.Github;

public class GithubUpdatePrModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; set; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; set; }
    
    [JsonPropertyName("pull_number"), JsonProperty("pull_number")]
    public int PrNumber { get; set; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public string State { get; set; }
}