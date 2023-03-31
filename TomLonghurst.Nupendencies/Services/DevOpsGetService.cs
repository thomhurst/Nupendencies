using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsGetService : IDevOpsGetService
{
    private readonly VssConnection _vssConnection;
    private readonly AzureDevOpsOptions _azureDevOpsOptions;

    public DevOpsGetService(VssConnection vssConnection, AzureDevOpsOptions azureDevOpsOptions)
    {
        _vssConnection = vssConnection;
        _azureDevOpsOptions = azureDevOpsOptions;
    }

    public async Task<IEnumerable<GitRepository>> GetRepositories()
    {
        var azureRepos = await _vssConnection.GetClient<GitHttpClient>().GetRepositoriesAsync(_azureDevOpsOptions.ProjectGuid);
        return azureRepos
            .Where(x => x.IsDisabled != true)
            .Select(MapRepo)
            .ToList();
    }

    public async Task<IEnumerable<Pr>> GetOpenPullRequests(string repoId)
    {
        var devOpsPullRequests = await _vssConnection.GetClient<GitHttpClient>().GetPullRequestsAsync(_azureDevOpsOptions.ProjectGuid, repoId, new GitPullRequestSearchCriteria(), top: 100);
        return devOpsPullRequests
            .Where(x => x.Status == PullRequestStatus.Active)
            .Select(MapPullRequest)
            .ToList();
    }

    private async Task<IEnumerable<Iss>> GetIssues()
    {
        // TODO var issues = await _devOpsHttpClient.GetUserStories();
        return new List<Iss>();
    }

    private Pr MapPullRequest(GitPullRequest arg)
    {
        return new Pr
        {
            Body = arg.Description,
            Id = arg.PullRequestId.ToString(),
            Number = arg.PullRequestId,
            Title = arg.Title,
            HasConflicts = arg.MergeStatus == PullRequestAsyncStatus.Conflicts
        };
    }

    private GitRepository MapRepo(global::Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository devOpsGitRepository)
    {
        return new GitRepository(RepositoryType.AzureDevOps)
        {
            Id = devOpsGitRepository.Id.ToString(),
            Name = devOpsGitRepository.Name,
            Owner = devOpsGitRepository.ProjectReference.Name,
            GitUrl = devOpsGitRepository.RemoteUrl,
            Issues = new List<Iss>(),
            IsDisabled = false,
            MainBranch = devOpsGitRepository.DefaultBranch
        };
    }
}