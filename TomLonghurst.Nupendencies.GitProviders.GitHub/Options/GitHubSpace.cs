namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

public abstract record GitHubSpace
{
    protected internal GitHubSpace()
    {
    }

    public static GitHubTeamSpace FromTeam(string organization, string team) => new()
    {
        Organization = organization,
        TeamName = team
    };
    
    public static GitHubOrganizationSpace FromOrganization(string organization) => new()
    {
        Organization = organization,
    };
    
    public static GitHubCurrentUserSpace FromCurrentUser() => new();
    
    public static GitHubUserSpace FromUser(string username) => new(username);
}