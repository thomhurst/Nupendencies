using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public class DevOpsUpdateIssueModel
{
    [JsonPropertyName("owner"), JsonProperty("owner")]
    public string Owner { get; set; }
    
    [JsonPropertyName("repo"), JsonProperty("repo")]
    public string Repo { get; set; }
    
    [JsonPropertyName("issue_number"), JsonProperty("issue_number")]
    public int IssueNumber { get; set; }

    [JsonPropertyName("state"), JsonProperty("state")]
    public string State { get; set; }
}