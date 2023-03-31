using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public interface IGitHubPullRequestService
{
    Task ClosePullRequest(GitRepository gitRepository, GitPullRequest pullRequest);
    Task CreatePullRequest(CreatePullRequestModel createPullRequestModel);
    Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository);
}