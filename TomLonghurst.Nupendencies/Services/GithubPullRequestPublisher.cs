using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class GithubPullRequestPublisher : BasePullRequestPublisher
{
    private readonly IGithubGetService _githubGetService;
    private readonly GithubHttpClient _githubHttpClient;
    
    public GithubPullRequestPublisher(NupendenciesOptions nupendenciesOptions, 
        GithubHttpClient githubHttpClient, 
        IGithubGetService githubGetService,
        IGitCredentialsProvider gitCredentialsProvider,
        ILogger<GithubPullRequestPublisher> logger) : base(nupendenciesOptions, gitCredentialsProvider, logger)
    {
        _githubHttpClient = githubHttpClient;
        _githubGetService = githubGetService;
    }

    protected override async Task<IEnumerable<Pr>> GetOpenPullRequests(Repo repo)
    {
        return await _githubGetService.GetOpenPullRequests(repo.Owner, repo.Name);
    }

    protected override async Task CreatePullRequest(Repo repo, string branchName, string body, int updateCount)
    {
        await _githubHttpClient.CreatePullRequest(repo.Owner, repo.Name, GenerateTitle(updateCount), body, branchName, repo.MainBranch);
    }

    protected override async Task ClosePullRequest(Repo repo, Pr pr)
    {
        await _githubHttpClient.ClosePr(repo.Owner, repo.Name, pr.Number);
    }

    protected override bool ShouldProcess(Repo repo)
    {
        return repo.RepositoryType == RepositoryType.Github;
    }
}