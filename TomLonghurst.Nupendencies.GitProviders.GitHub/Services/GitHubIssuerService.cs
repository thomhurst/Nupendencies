using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using Octokit.GraphQL.Model;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubIssuerService : IGitHubIssuerService
{
    private readonly GitHubHttpClient _githubHttpClient;
    private readonly IGitHubGraphQlClientProvider _graphQlClientProvider;
    private readonly GitHubOptions _gitHubOptions;

    public GitHubIssuerService(GitHubHttpClient githubHttpClient,
        IGitHubGraphQlClientProvider graphQlClientProvider,
        GitHubOptions gitHubOptions)
    {
        _githubHttpClient = githubHttpClient;
        _graphQlClientProvider = graphQlClientProvider;
        _gitHubOptions = gitHubOptions;
    }

    public async Task<IList<GitIssue>> GetCurrentIssues(GitRepository gitRepository)
    {
        var query = GetIssuesQueryableList(gitRepository)
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

    private IQueryableList<Issue> GetIssuesQueryableList(GitRepository gitRepository)
    {
        if (_gitHubOptions.GitHubSpace is GitHubUserSpace)
        {
            return new Query()
                .User(gitRepository.Owner)
                .Repository(gitRepository.Name)
                .Issues(null, null, null, null, null, null, null, null)
                .AllPages();
        }
        
        return new Query()
            .Organization(gitRepository.Owner)
            .Repository(gitRepository.Name)
            .Issues(null, null, null, null, null, null, null, null)
            .AllPages();
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