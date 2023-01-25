using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public class WorkItemRef
{
    [JsonPropertyName("id"), JsonProperty("id")]
    public string Id { get; set; }
}