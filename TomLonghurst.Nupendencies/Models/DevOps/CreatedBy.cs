using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record CreatedBy(
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("uniqueName")] string UniqueName,
    [property: JsonPropertyName("imageUrl")] string ImageUrl,
    [property: JsonPropertyName("descriptor")] string Descriptor,
    [property: JsonPropertyName("href")] string Href
);