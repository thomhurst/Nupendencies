using Octokit;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub.Services;

public class GitHubPullRequestService : IGitHubPullRequestService
{
    private readonly IGitHubClientProvider _gitHubClientProvider;

    public GitHubPullRequestService(IGitHubClientProvider gitHubClientProvider)
    {
        _gitHubClientProvider = gitHubClientProvider;
    }

    public async Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        await _gitHubClientProvider.GitHubClient.PullRequest.Create(long.Parse(createPullRequestModel.Repository.Id),
            new NewPullRequest(createPullRequestModel.Title, createPullRequestModel.HeadBranch,
                createPullRequestModel.BaseBranch)
            {
                Body = createPullRequestModel.Body
            });
    }

    public async Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository)
    {
        var pullRequests = await _gitHubClientProvider.GitHubClient.Repository
            .PullRequest
            .GetAllForRepository(long.Parse(repository.Id));

        return pullRequests.Select(p => new GitPullRequest
        {
            Number = p.Number,
            Id = p.Id.ToString(),
            Title = p.Title,
            Body = p.Body,
            HasConflicts = p.MergeableState is { Value: MergeableState.Dirty }
        }).ToList();
    }

    public async Task ClosePullRequest(GitRepository gitRepository, GitPullRequest pullRequest)
    {
        await _gitHubClientProvider.GitHubClient.PullRequest.Update(long.Parse(gitRepository.Id), pullRequest.Number,
            new PullRequestUpdate
            {
                State = ItemState.Closed
            });
    }
}