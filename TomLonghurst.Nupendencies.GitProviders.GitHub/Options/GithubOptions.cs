namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public record GitHubOptions
{
    public required string AuthenticationUsername { get; init; }
    public required string AuthenticationPatToken { get; init; }
    public required GitHubSpace GitHubSpace { get; init; }
}