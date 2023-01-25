using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models.DevOps;

public record Self(
    [property: JsonPropertyName("href")] string Href
);