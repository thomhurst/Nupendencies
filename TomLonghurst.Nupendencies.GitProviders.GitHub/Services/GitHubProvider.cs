using LibGit2Sharp;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubProvider : IGitProvider
{
    private readonly IGitHubIssueService _gitHubIssueService;
    private readonly IGitHubPullRequestService _gitHubPullRequestService;
    private readonly GitHubOptions _githubOptions;
    private readonly IGitHubRepositoriesProvider _gitHubRepositoriesProvider;

    public GitHubProvider(IGitHubIssueService gitHubIssueService,
        IGitHubPullRequestService gitHubPullRequestService,
        GitHubOptions githubOptions,
        IGitHubRepositoriesProvider gitHubRepositoriesProvider)
    {
        _gitHubIssueService = gitHubIssueService;
        _gitHubPullRequestService = gitHubPullRequestService;
        _githubOptions = githubOptions;
        _gitHubRepositoriesProvider = gitHubRepositoriesProvider;
    }

    public async Task<IEnumerable<GitRepository>> GetRepositories()
    {
        var githubRepos = await _gitHubRepositoriesProvider.GetRepositories();

        var repositories = await githubRepos
            .ToAsyncProcessorBuilder()
            .SelectAsync(async r => new GitRepository
            {
                Provider = this,
                Credentials = new UsernamePasswordCredentials
                {
                    Username = _githubOptions.AuthenticationUsername,
                    Password = _githubOptions.AuthenticationPatToken
                },
                Owner = r.Owner.Login,
                Name = r.Name,
                Id = r.Id.ToString(),
                IsDisabled = r.Archived,
                GitUrl = r.SshUrl.Replace("git@github.com:", "https://github.com/"),
                MainBranch = r.DefaultBranch,
                Issues = await _gitHubIssueService.GetIssues(r.Id)
            })
            .ProcessInParallel(25, TimeSpan.FromSeconds(1));
        
        return repositories;
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
        return await _gitHubIssueService.GetIssues(long.Parse(repository.Id));
    }

    public async Task CreateIssue(GitRepository repository, string title, string body)
    {
        await _gitHubIssueService.RaiseIssue(repository, title, body);
    }

    public async Task CloseIssue(GitRepository repository, GitIssue issue)
    {
        await _gitHubIssueService.CloseIssue(repository, issue);
    }
}