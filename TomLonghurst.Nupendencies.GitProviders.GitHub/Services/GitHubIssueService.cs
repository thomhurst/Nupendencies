using Octokit;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubIssueService : IGitHubIssueService
{
    private readonly IGitHubClientProvider _gitHubClientProvider;

    public GitHubIssueService(IGitHubClientProvider gitHubClientProvider)
    {
        _gitHubClientProvider = gitHubClientProvider;
    }
    
    public async Task<List<GitIssue>> GetIssues(long repositoryId)
    {
        var issues = await _gitHubClientProvider.GitHubClient.Issue.GetAllForRepository(repositoryId);
        
        return issues
            .Select(x => new GitIssue
            {
                Author = x.User.Name,
                Created = x.CreatedAt,
                Id = x.Id.ToString(),
                IsClosed = x.ClosedAt != null,
                IssueNumber = x.Number,
                LastUpdated = x.UpdatedAt ?? x.CreatedAt,
                Title = x.Title
            }).ToList();
    }

    public async Task RaiseIssue(GitRepository gitRepository, string title, string body)
    {
        await _gitHubClientProvider.GitHubClient.Issue.Create(long.Parse(gitRepository.Id), new NewIssue(title)
        {
            Body = body
        });
    }

    public async Task CloseIssue(GitRepository gitRepository, GitIssue issue)
    {
        await _gitHubClientProvider.GitHubClient.Issue.Update(long.Parse(gitRepository.Id), issue.IssueNumber, new IssueUpdate
        {
            State = ItemState.Closed
        });
    }
}