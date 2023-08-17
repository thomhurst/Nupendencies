using Octokit;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public interface IGitHubRepositoriesProvider
{
    Task<IReadOnlyList<Repository>> GetRepositories();
}