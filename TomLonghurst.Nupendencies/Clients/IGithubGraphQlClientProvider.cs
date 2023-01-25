using Octokit.GraphQL;

namespace TomLonghurst.Nupendencies.Clients;

public interface IGithubGraphQlClientProvider
{
    Connection GithubGraphQlClient { get; }
}