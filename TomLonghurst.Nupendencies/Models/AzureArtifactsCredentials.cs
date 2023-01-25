using System.Text.Json.Serialization;

namespace TomLonghurst.Nupendencies.Models;

public record AzureArtifactsCredentials(
    [property: JsonPropertyName("endpointCredentials")] IReadOnlyList<EndpointCredential> EndpointCredentials
);

