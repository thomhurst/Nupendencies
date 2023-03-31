using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Models;

public class DevOpsUpdatePrModel
{
    [JsonPropertyName("status"), JsonProperty("status")]
    public string Status { get; init; }
}