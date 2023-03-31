using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public interface IGitHubIssuerService
{
    Task<IList<GitIssue>> GetCurrentIssues(GitRepository gitRepository);
    Task RaiseIssue(GitRepository gitRepository, string title, string body);
    Task CloseIssue(GitRepository gitRepository, GitIssue issue);
}