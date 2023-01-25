using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record DevOpsTeamMember(
    [property: JsonPropertyName("identity")] Identity Identity,
    [property: JsonPropertyName("isTeamAdmin")] bool? IsTeamAdmin
);