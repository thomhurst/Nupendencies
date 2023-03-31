using LibGit2Sharp;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.AzureDevOps.Options;
using GitPullRequest = TomLonghurst.Nupendencies.Abstractions.Models.GitPullRequest;
using GitRepository = TomLonghurst.Nupendencies.Abstractions.Models.GitRepository;

namespace TomLonghurst.Nupendencies.GitProviders.AzureDevOps;

public class AzureDevOpsProvider : IGitProvider
{
    private readonly VssConnection _vssConnection;
    private readonly IDevOpsPullRequestService _pullRequestService;
    private readonly AzureDevOpsOptions _azureDevOpsOptions;

    public AzureDevOpsProvider(VssConnection vssConnection,
        IDevOpsPullRequestService pullRequestService,
        AzureDevOpsOptions azureDevOpsOptions)
    {
        _vssConnection = vssConnection;
        _pullRequestService = pullRequestService;
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

    public Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository)
    {
        return _pullRequestService.GetOpenPullRequests(repository);
    }

    public Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        return _pullRequestService.CreatePullRequest(createPullRequestModel);
    }

    public Task ClosePullRequest(GitRepository repository, GitPullRequest pullRequest)
    {
        return _pullRequestService.ClosePullRequest(repository, pullRequest);
    }

    public Task<IEnumerable<GitIssue>> GetOpenIssues(GitRepository repository)
    {
        // TODO - Issues isn't a concept on DevOps. Work Items?
        return Task.FromResult<IEnumerable<GitIssue>>(Array.Empty<GitIssue>());
    }

    public Task CreateIssue(GitRepository repository, string title, string body)
    {
        // TODO - Issues isn't a concept on DevOps. Work Items?
        return Task.CompletedTask;
    }

    public Task CloseIssue(GitRepository repository, GitIssue issue)
    {
        // TODO - Issues isn't a concept on DevOps. Work Items?
        return Task.CompletedTask;
    }

    private GitRepository MapRepo(global::Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository devOpsGitRepository)
    {
        return new GitRepository
        {
            Provider = this,
            Credentials = new UsernamePasswordCredentials
            {
                Username = _azureDevOpsOptions.AuthenticationUsername,
                Password = _azureDevOpsOptions.AuthenticationPatToken
            },
            Id = devOpsGitRepository.Id.ToString(),
            Name = devOpsGitRepository.Name,
            Owner = devOpsGitRepository.ProjectReference.Name,
            GitUrl = devOpsGitRepository.RemoteUrl,
            Issues = new List<GitIssue>(),
            IsDisabled = false,
            MainBranch = devOpsGitRepository.DefaultBranch
        };
    }

    public int PullRequestBodyCharacterLimit => 400;
}