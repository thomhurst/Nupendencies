using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public class GitHubPullRequestService : IGitHubPullRequestService
{
    private readonly GitHubHttpClient _githubHttpClient;
    private readonly IGitHubGraphQlClientProvider _gitHubGraphQlClientProvider;

    public GitHubPullRequestService(GitHubHttpClient githubHttpClient,
        IGitHubGraphQlClientProvider gitHubGraphQlClientProvider)
    {
        _githubHttpClient = githubHttpClient;
        _gitHubGraphQlClientProvider = gitHubGraphQlClientProvider;
    }

    public async Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        await _githubHttpClient.CreatePullRequest(createPullRequestModel.Repository.Owner, createPullRequestModel.Repository.Name, createPullRequestModel.Title, createPullRequestModel.Body, createPullRequestModel.HeadBranch, createPullRequestModel.BaseBranch);
    }

    public async Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository)
    {
        var query = new Query()
            .Repository(repository.Name, repository.Owner, true)
            .PullRequests(states: new List<PullRequestState> { PullRequestState.Open })
            .AllPages()
            .Select(p => new GitPullRequest
            {
                Number = p.Number,
                Id = p.Id.Value,
                Title = p.Title,
                Body = p.Body,
                HasConflicts = p.Mergeable == MergeableState.Conflicting
            }).Compile();

        return await _gitHubGraphQlClientProvider.GitHubGraphQlClient.Run(query);
    }

    public async Task ClosePullRequest(GitRepository gitRepository, GitPullRequest pullRequest)
    {
        await _githubHttpClient.ClosePr(gitRepository.Owner, gitRepository.Name, pullRequest.Number);
    }
}