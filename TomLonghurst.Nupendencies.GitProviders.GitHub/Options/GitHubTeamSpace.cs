namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public sealed record GitHubTeamSpace : GitHubSpace
{
    public required string Organization { get; init; }
    public required string TeamName { get; init; }
}