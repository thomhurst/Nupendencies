namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public record GitHubOptions
{
    public required string AuthenticationUsername { get; set; }
    public required string AuthenticationPatToken { get; set; }
    public required GitHubSpace GitHubSpace { get; set; }
}