using Octokit.GraphQL;
using TomLonghurst.Nupendencies.Abstractions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public class GitHubIssuerService : IGitHubIssuerService
{
    private readonly GitHubHttpClient _githubHttpClient;
    private readonly IGitHubGraphQlClientProvider _graphQlClientProvider;

    public GitHubIssuerService(GitHubHttpClient githubHttpClient,
        IGitHubGraphQlClientProvider graphQlClientProvider)
    {
        _githubHttpClient = githubHttpClient;
        _graphQlClientProvider = graphQlClientProvider;
    }

    public async Task<IList<GitIssue>> GetCurrentIssues(GitRepository gitRepository)
    {
        var query = new Query()
            .Organization(gitRepository.Owner)
            .Repository(gitRepository.Name)
            .Issues(null, null, null, null, null, null, null, null)
            .AllPages()
            .Select(x => new GitIssue
            {
                IssueNumber = x.Number,
                Id = x.Id.Value,
                Author = x.Author.Login,
                Created = x.CreatedAt,
                Title = x.Title,
                LastUpdated = x.UpdatedAt,
                IsClosed = x.Closed
            });

        var issues = await _graphQlClientProvider.GitHubGraphQlClient.Run(query);

        return issues.ToList();
    }

    public async Task RaiseIssue(GitRepository gitRepository, string title, string body)
    {
        await _githubHttpClient.CreateIssue(gitRepository.Owner,
            gitRepository.Name,
            title,
            body
        );
    }

    public async Task CloseIssue(GitRepository gitRepository, GitIssue issue)
    {
        await _githubHttpClient.CloseIssue(gitRepository.Owner, gitRepository.Name, issue.IssueNumber);
    }
}