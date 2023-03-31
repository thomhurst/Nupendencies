using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps;

public interface IDevOpsPullRequestService
{
    Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository);
    Task CreatePullRequest(CreatePullRequestModel createPullRequestModel);
    Task ClosePullRequest(GitRepository repository, GitPullRequest pullRequest);
}