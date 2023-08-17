using Octokit;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubRepositoriesProvider : IGitHubRepositoriesProvider
{
    private readonly IGitHubClientProvider _gitHubClientProvider;
    private readonly GitHubOptions _gitHubOptions;

    public GitHubRepositoriesProvider(IGitHubClientProvider gitHubClientProvider, GitHubOptions gitHubOptions)
    {
        _gitHubClientProvider = gitHubClientProvider;
        _gitHubOptions = gitHubOptions;
    }
    
    public async Task<IReadOnlyList<Repository>> GetRepositories()
    {
        var gitHubClient = _gitHubClientProvider.GitHubClient;
        
        if (_gitHubOptions.GitHubSpace is GitHubOrganizationSpace gitHubOrganizationSpace)
        {
            return await gitHubClient.Repository.GetAllForOrg(
                gitHubOrganizationSpace.Organization);
        }
        
        if (_gitHubOptions.GitHubSpace is GitHubCurrentUserSpace)
        {
            return await gitHubClient.Repository.GetAllForCurrent();
        }
        
        if (_gitHubOptions.GitHubSpace is GitHubUserSpace gitHubUserSpace)
        {
            return await gitHubClient.Repository.GetAllForUser(gitHubUserSpace.Username);
        }
        
        if (_gitHubOptions.GitHubSpace is GitHubTeamSpace gitHubTeamSpace)
        {
            var team = await gitHubClient.Organization.Team.GetByName(gitHubTeamSpace.Organization, gitHubTeamSpace.TeamName);
            return await gitHubClient.Organization.Team.GetAllRepositories(team.Id);
        }

        throw new ArgumentOutOfRangeException(nameof(_gitHubOptions.GitHubSpace));
    }
}