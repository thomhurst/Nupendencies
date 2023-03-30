using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsPullRequestPublisher : BasePullRequestPublisher
{
    private readonly IDevOpsGetService _devOpsGetService;
    private readonly VssConnection _vssConnection;
    private readonly AzureDevOpsOptions _azureDevOpsOptions;

    public DevOpsPullRequestPublisher(NupendenciesOptions nupendenciesOptions, 
        IDevOpsGetService devOpsGetService,
        IGitCredentialsProvider gitCredentialsProvider,
        ILogger<DevOpsPullRequestPublisher> logger,
        VssConnection vssConnection,
        AzureDevOpsOptions azureDevOpsOptions) : base(nupendenciesOptions, gitCredentialsProvider, logger)
    {
        _devOpsGetService = devOpsGetService;
        _vssConnection = vssConnection;
        _azureDevOpsOptions = azureDevOpsOptions;
    }

    protected override async Task<IEnumerable<Pr>> GetOpenPullRequests(GitRepository gitRepository)
    {
        return await _devOpsGetService.GetOpenPullRequests(gitRepository.Id);
    }

    protected override async Task CreatePullRequest(GitRepository gitRepository, string branchName, string body, int updateCount)
    {
        await _vssConnection.GetClient<GitHttpClient>().CreatePullRequestAsync(new GitPullRequest()
            {
                Title = GenerateTitle(updateCount),
                Description = body,
                TargetRefName = gitRepository.MainBranch,
                SourceRefName = $"refs/heads/{branchName}",
                WorkItemRefs = _nupendenciesOptions.AzureDevOpsOptions.WorkItemIds?.Select(workItemId => new ResourceRef { Id = workItemId }).ToArray()
            },
            project: _azureDevOpsOptions.ProjectGuid,
            repositoryId: gitRepository.Id);
    }

    protected override async Task ClosePullRequest(GitRepository gitRepository, Pr pr)
    {
        await _vssConnection.GetClient<GitHttpClient>().UpdatePullRequestAsync(new GitPullRequest()
            {
                Status = PullRequestStatus.Abandoned
            },
            project: _azureDevOpsOptions.ProjectGuid,
            repositoryId: gitRepository.Id,
            pullRequestId: pr.Number);
    }

    protected override bool ShouldProcess(GitRepository gitRepository)
    {
        return gitRepository.RepositoryType == RepositoryType.AzureDevOps;
    }
}