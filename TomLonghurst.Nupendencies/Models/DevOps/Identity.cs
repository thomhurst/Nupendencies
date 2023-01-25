using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record Identity(
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("imageUrl")] string ImageUrl
);