using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsTeamWrapper(
    [property: JsonPropertyName("value")] IReadOnlyList<DevOpsTeam> Value,
    [property: JsonPropertyName("count")] int Count
);