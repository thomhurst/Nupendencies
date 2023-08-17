using Octokit;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

public interface IGitHubClientProvider
{
    GitHubClient GitHubClient { get; }
}