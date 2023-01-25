using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record Reviewer(
    [property: JsonPropertyName("vote")] int Vote,
    [property: JsonPropertyName("hasDeclined")] bool HasDeclined,
    [property: JsonPropertyName("isFlagged")] bool IsFlagged,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("votedFor")] IReadOnlyList<VotedFor> VotedFor,
    [property: JsonPropertyName("isRequired")] bool? IsRequired
);