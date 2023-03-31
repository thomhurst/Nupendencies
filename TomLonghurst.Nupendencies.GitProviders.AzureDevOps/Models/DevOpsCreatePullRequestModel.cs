using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Models;

public class DevOpsCreatePullRequestModel
{
    [JsonPropertyName("title"), JsonProperty("title")]
    public string Title { get; set; }

    [JsonPropertyName("description"), JsonProperty("description")]
    public string Body { get; set; }
    
    [JsonPropertyName("targetRefName"), JsonProperty("targetRefName")]
    public string TargetBranch { get; set; }
    
    [JsonPropertyName("sourceRefName"), JsonProperty("sourceRefName")]
    public string SourceBranch { get; set; }
    
    [JsonPropertyName("workItemRefs"), JsonProperty("workItemRefs")]
    public WorkItemRef[] WorkItemRefs { get; set; }
}