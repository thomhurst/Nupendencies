using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;

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

    protected override async Task<IEnumerable<Pr>> GetOpenPullRequests(GitRepository gitRepository)
    {
        return await _githubGetService.GetOpenPullRequests(gitRepository.Owner, gitRepository.Name);
    }

    protected override async Task CreatePullRequest(GitRepository gitRepository, string branchName, string body, int updateCount)
    {
        await _githubHttpClient.CreatePullRequest(gitRepository.Owner, gitRepository.Name, GenerateTitle(updateCount), body, branchName, gitRepository.MainBranch);
    }

    protected override async Task ClosePullRequest(GitRepository gitRepository, Pr pr)
    {
        await _githubHttpClient.ClosePr(gitRepository.Owner, gitRepository.Name, pr.Number);
    }

    protected override bool ShouldProcess(GitRepository gitRepository)
    {
        return gitRepository.RepositoryType == RepositoryType.Github;
    }
}