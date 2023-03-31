using Octokit.GraphQL;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

public interface IGitHubGraphQlClientProvider
{
    Connection GitHubGraphQlClient { get; }
}