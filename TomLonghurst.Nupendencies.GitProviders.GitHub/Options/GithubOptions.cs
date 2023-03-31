namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public class GitHubOptions
{
    public required string AuthenticationUsername { get; init; }
    public required string AuthenticationPatToken { get; init; }
    public required string Organization { get; init; }
    public required string Team { get; init; }
}