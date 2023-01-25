using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class GithubIssuerRaiserService : BaseIssuerRaiserService
{
    private readonly GithubHttpClient _githubHttpClient;

    public GithubIssuerRaiserService(GithubHttpClient githubHttpClient, ILogger<GithubIssuerRaiserService> logger) : base(logger)
    {
        _githubHttpClient = githubHttpClient;
    }

    protected override async Task RaiseIssue(Repo repo, PackageUpdateResult packageUpdateResult)
    {
        await _githubHttpClient.CreateIssue(repo.Owner,
            repo.Name,
            GenerateTitle(packageUpdateResult),
            GenerateBody(packageUpdateResult)
        );
    }

    protected override async Task CloseIssue(Repo repo, Iss issue)
    {
        await _githubHttpClient.CloseIssue(repo.Owner, repo.Name, issue.IssueNumber);
    }

    protected override bool ShouldProcess(Repo repo)
    {
        return repo.RepositoryType == RepositoryType.Github;
    }
}