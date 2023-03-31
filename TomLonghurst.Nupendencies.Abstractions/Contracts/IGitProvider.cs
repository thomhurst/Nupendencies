using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Abstractions.Contracts;

public interface IGitProvider
{
    int PullRequestBodyCharacterLimit => int.MaxValue;
    Task<IEnumerable<GitRepository>> GetRepositories();
    
    Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository);
    Task CreatePullRequest(CreatePullRequestModel createPullRequestModel);
    Task ClosePullRequest(GitRepository repository, GitPullRequest pullRequest);

    Task<IEnumerable<GitIssue>> GetOpenIssues(GitRepository repository);
    Task CreateIssue(GitRepository repository, string title, string body);
    Task CloseIssue(GitRepository repository, GitIssue issue);
}