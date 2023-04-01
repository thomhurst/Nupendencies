namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public sealed record GitHubTeamSpace : GitHubSpace
{
    public required string Organization { get; set; }
    public required string TeamName { get; set; }
}