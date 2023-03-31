using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;
using GitPullRequest = Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequest;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps;

public class DevOpsPullRequestService : IDevOpsPullRequestService
{
    private readonly VssConnection _vssConnection;
    private readonly AzureDevOpsOptions _azureDevOpsOptions;

    public DevOpsPullRequestService(VssConnection vssConnection,
        AzureDevOpsOptions azureDevOpsOptions)
    {
        _vssConnection = vssConnection;
        _azureDevOpsOptions = azureDevOpsOptions;
    }

    public async Task<IEnumerable<Abstractions.Models.GitPullRequest>> GetOpenPullRequests(Abstractions.Models.GitRepository repository)
    {
        var devOpsPullRequests = await _vssConnection.GetClient<GitHttpClient>().GetPullRequestsAsync(_azureDevOpsOptions.ProjectGuid, repository.Id, new GitPullRequestSearchCriteria(), top: 100);
        return devOpsPullRequests
            .Where(x => x.Status == PullRequestStatus.Active)
            .Select(MapPullRequest)
            .ToList();
    }

    public async Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        await _vssConnection.GetClient<GitHttpClient>().CreatePullRequestAsync(new GitPullRequest()
            {
                Title = createPullRequestModel.Title,
                Description = createPullRequestModel.Body,
                TargetRefName = createPullRequestModel.BaseBranch,
                SourceRefName = createPullRequestModel.HeadBranch,
                WorkItemRefs = _azureDevOpsOptions.WorkItemIds?.Select(workItemId => new ResourceRef { Id = workItemId }).ToArray()
            },
            project: _azureDevOpsOptions.ProjectGuid,
            repositoryId: createPullRequestModel.Repository.Id);
    }

    public async Task ClosePullRequest(Abstractions.Models.GitRepository repository, Abstractions.Models.GitPullRequest pullRequest)
    {
        await _vssConnection.GetClient<GitHttpClient>().UpdatePullRequestAsync(new GitPullRequest
            {
                Status = PullRequestStatus.Abandoned
            },
            project: _azureDevOpsOptions.ProjectGuid,
            repositoryId: repository.Id,
            pullRequestId: pullRequest.Number);
    }

    private Abstractions.Models.GitPullRequest MapPullRequest(GitPullRequest pullRequest)
    {
        return new Abstractions.Models.GitPullRequest
        {
            Body = pullRequest.Description,
            Id = pullRequest.PullRequestId.ToString(),
            Number = pullRequest.PullRequestId,
            Title = pullRequest.Title,
            HasConflicts = pullRequest.MergeStatus == PullRequestAsyncStatus.Conflicts
        };
    }
}