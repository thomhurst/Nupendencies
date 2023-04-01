using LibGit2Sharp;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using Octokit.GraphQL.Model;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;
using Repository = Octokit.GraphQL.Model.Repository;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubProvider : IGitProvider
{
    private readonly IGitHubGraphQlClientProvider _githubGraphQlClientProvider;
    private readonly IGitHubIssuerService _gitHubIssuerService;
    private readonly IGitHubPullRequestService _gitHubPullRequestService;
    private readonly GitHubOptions _githubOptions;

    public GitHubProvider(IGitHubGraphQlClientProvider githubGraphQlClientProvider,
        IGitHubIssuerService gitHubIssuerService,
        IGitHubPullRequestService gitHubPullRequestService,
        GitHubOptions githubOptions)
    {
        _githubGraphQlClientProvider = githubGraphQlClientProvider;
        _gitHubIssuerService = gitHubIssuerService;
        _gitHubPullRequestService = gitHubPullRequestService;
        _githubOptions = githubOptions;
    }

    public async Task<IEnumerable<GitRepository>> GetRepositories()
    {
        var query = GetRepositoryQueryableList()
            .Select(r => new GitRepository
            {
                Provider = this,
                Credentials = new UsernamePasswordCredentials
                {
                    Username = _githubOptions.AuthenticationUsername,
                    Password = _githubOptions.AuthenticationPatToken
                },
                Owner = r.Owner.Login,
                Name = r.Name,
                Id = r.Id.Value,
                IsDisabled = r.IsDisabled || r.IsArchived,
                GitUrl = r.SshUrl,
                MainBranch = r.DefaultBranchRef.Name,
                Issues = r.Issues(null, null, null, null, null, null, new IssueOrder
                        {
                            Direction = OrderDirection.Desc,
                            Field = IssueOrderField.UpdatedAt
                        },
                        new List<IssueState> { IssueState.Open })
                    .AllPages()
                    .Select(i => new GitIssue
                    {
                        IssueNumber = i.Number,
                        Id = i.Id.Value,
                        Title = i.Title,
                        Author = i.Author.Login,
                        Created = i.CreatedAt,
                        LastUpdated = i.UpdatedAt,
                        IsClosed = i.Closed,
                    })
                    .ToList()
            }).Compile();

        var repositories = (await _githubGraphQlClientProvider.GitHubGraphQlClient.Run(query)).ToList();
        
        repositories.ForEach(r => r.GitUrl = r.GitUrl.Replace("git@github.com:", "https://github.com/"));
        
        return repositories;
    }

    private IQueryableList<Repository> GetRepositoryQueryableList()
    {
        if (_githubOptions.GitHubSpace is GitHubTeamSpace gitHubTeamSpace)
        {
            return new Query()
                .Organization(gitHubTeamSpace.Organization)
                .Team(gitHubTeamSpace.TeamName)
                .Repositories()
                .AllPages();
        }
        
        return new Query()
            .User(_githubOptions.AuthenticationUsername)
            .Repositories()
            .AllPages();
    }

    public async Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository)
    {
        return await _gitHubPullRequestService.GetOpenPullRequests(repository);
    }

    public async Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        await _gitHubPullRequestService.CreatePullRequest(createPullRequestModel);
    }

    public async Task ClosePullRequest(GitRepository repository, GitPullRequest pullRequest)
    {
        await _gitHubPullRequestService.ClosePullRequest(repository, pullRequest);
    }

    public async Task<IEnumerable<GitIssue>> GetOpenIssues(GitRepository repository)
    {
        return await _gitHubIssuerService.GetCurrentIssues(repository);
    }

    public async Task CreateIssue(GitRepository repository, string title, string body)
    {
        await _gitHubIssuerService.RaiseIssue(repository, title, body);
    }

    public async Task CloseIssue(GitRepository repository, GitIssue issue)
    {
        await _gitHubIssuerService.CloseIssue(repository, issue);
    }
}