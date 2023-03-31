using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Models;

public class WorkItemRef
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; }
}