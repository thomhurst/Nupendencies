using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public class DevOpsUpdatePrModel
{
    [JsonPropertyName("status"), JsonProperty("status")]
    public string Status { get; set; }
}