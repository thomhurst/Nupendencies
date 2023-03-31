using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Models;

public class DevOpsCreatePullRequestModel
{
    [JsonPropertyName("title"), JsonProperty("title")]
    public string Title { get; init; }

    [JsonPropertyName("description"), JsonProperty("description")]
    public string Body { get; init; }
    
    [JsonPropertyName("targetRefName"), JsonProperty("targetRefName")]
    public string TargetBranch { get; init; }
    
    [JsonPropertyName("sourceRefName"), JsonProperty("sourceRefName")]
    public string SourceBranch { get; init; }
    
    [JsonPropertyName("workItemRefs"), JsonProperty("workItemRefs")]
    public WorkItemRef[] WorkItemRefs { get; init; }
}