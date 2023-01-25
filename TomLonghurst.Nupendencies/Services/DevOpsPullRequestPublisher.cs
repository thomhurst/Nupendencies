using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsPullRequestPublisher : BasePullRequestPublisher
{
    private readonly IDevOpsGetService _devOpsGetService;
    private readonly DevOpsHttpClient _devOpsHttpClient;

    public DevOpsPullRequestPublisher(NupendenciesOptions nupendenciesOptions, 
        IDevOpsGetService devOpsGetService, 
        DevOpsHttpClient devOpsHttpClient,
        IGitCredentialsProvider gitCredentialsProvider,
        ILogger<DevOpsPullRequestPublisher> logger) : base(nupendenciesOptions, gitCredentialsProvider, logger)
    {
        _devOpsGetService = devOpsGetService;
        _devOpsHttpClient = devOpsHttpClient;
    }

    protected override async Task<IEnumerable<Pr>> GetOpenPullRequests(Repo repo)
    {
        return await _devOpsGetService.GetOpenPullRequests(repo.Id);
    }

    protected override async Task CreatePullRequest(Repo repo, string branchName, string body, int updateCount)
    {
        await _devOpsHttpClient.CreatePullRequest(repo.Id, GenerateTitle(updateCount), body, branchName, repo.MainBranch);
    }

    protected override async Task ClosePullRequest(Repo repo, Pr pr)
    {
        await _devOpsHttpClient.ClosePr(repo.Id, pr.Number);
    }

    protected override bool ShouldProcess(Repo repo)
    {
        return repo.RepositoryType == RepositoryType.AzureDevOps;
    }
}