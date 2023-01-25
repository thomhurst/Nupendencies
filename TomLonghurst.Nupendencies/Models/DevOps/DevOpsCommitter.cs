using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsCommitter(
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("name")] string Name
);