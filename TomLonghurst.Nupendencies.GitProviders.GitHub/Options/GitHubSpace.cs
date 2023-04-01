namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public abstract record GitHubSpace
{
    protected internal GitHubSpace()
    {
    }

    public static GitHubTeamSpace Team(string organization, string team) => new()
    {
        Organization = organization,
        TeamName = team
    };
    
    public static GitHubUserSpace User() => new();
}