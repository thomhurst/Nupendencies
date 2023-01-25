using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsTeamMembersResponseWrapper(
    [property: JsonPropertyName("value")] IReadOnlyList<DevOpsTeamMember> Value,
    [property: JsonPropertyName("count")] int Count
);