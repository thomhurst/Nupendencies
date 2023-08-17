using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public interface IGitHubIssueService
{
    Task<List<GitIssue>> GetIssues(long repositoryId);
    Task RaiseIssue(GitRepository gitRepository, string title, string body);
    Task CloseIssue(GitRepository gitRepository, GitIssue issue);
}