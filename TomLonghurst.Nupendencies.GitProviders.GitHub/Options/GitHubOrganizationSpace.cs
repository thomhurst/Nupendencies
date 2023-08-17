namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public sealed record GitHubOrganizationSpace : GitHubSpace
{
    public required string Organization { get; set; }
}