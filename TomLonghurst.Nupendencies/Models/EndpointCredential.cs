using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models;

public record EndpointCredential(
    [property: JsonPropertyName("endpoint")] string Endpoint,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password
);