using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsIssuerRaiserService : BaseIssuerRaiserService
{
    private readonly DevOpsHttpClient _devOpsHttpClient;

    public DevOpsIssuerRaiserService(DevOpsHttpClient devOpsHttpClient, ILogger<DevOpsIssuerRaiserService> logger) : base(logger)
    {
        _devOpsHttpClient = devOpsHttpClient;
    }

    protected override async Task RaiseIssue(Repo repo, PackageUpdateResult packageUpdateResult)
    {
        await _devOpsHttpClient.CreateIssue(repo.Id,
            GenerateTitle(packageUpdateResult),
            GenerateBody(packageUpdateResult)
        );
    }

    protected override async Task CloseIssue(Repo repo, Iss issue)
    {
        await _devOpsHttpClient.CloseIssue(repo.Id, issue.IssueNumber);
    }

    protected override bool ShouldProcess(Repo repo)
    {
        return false; // TODO: This hasn't been built yet
        return repo.RepositoryType == RepositoryType.AzureDevOps;
    }
}