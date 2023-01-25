using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Models.DevOps;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsGetService : IDevOpsGetService
{
    private readonly DevOpsHttpClient _devOpsHttpClient;

    public DevOpsGetService(DevOpsHttpClient devOpsHttpClient)
    {
        _devOpsHttpClient = devOpsHttpClient;
    }

    public async Task<IEnumerable<Repo>> GetRepositories()
    {
        var azureRepos = await _devOpsHttpClient.GetGitRepositories();

        return azureRepos.Select(MapRepo).ToList();
    }

    public async Task<IEnumerable<Pr>> GetOpenPullRequests(string repoId)
    {
        var devOpsPullRequests = await _devOpsHttpClient.GetPullRequestsForRepository(repoId);

        return devOpsPullRequests.Select(MapPullRequest).ToList();
    }

    private async Task<IEnumerable<Iss>> GetIssues()
    {
        // TODO var issues = await _devOpsHttpClient.GetUserStories();
        return new List<Iss>();
    }

    private Pr MapPullRequest(DevOpsPullRequest arg)
    {
        return new Pr
        {
            Body = arg.Description,
            Id = arg.PullRequestId.ToString(),
            Number = arg.PullRequestId,
            Title = arg.Title,
            HasConflicts = arg.MergeStatus == "conflicts"
        };
    }

    private Repo MapRepo(DevOpsGitRepository devOpsGitRepository)
    {
        return new Repo(RepositoryType.AzureDevOps)
        {
            Id = devOpsGitRepository.Id,
            Name = devOpsGitRepository.Name,
            Owner = devOpsGitRepository.Project.Name,
            GitUrl = devOpsGitRepository.RemoteUrl,
            Issues = new List<Iss>(),
            IsDisabled = false,
            MainBranch = devOpsGitRepository.DefaultBranch
        };
    }
}