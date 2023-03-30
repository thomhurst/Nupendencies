using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class DevOpsIssuerRaiserService : BaseIssuerRaiserService
{
    private readonly VssConnection _vssConnection;
    private readonly AzureDevOpsOptions _azureDevOpsOptions;

    public DevOpsIssuerRaiserService(ILogger<DevOpsIssuerRaiserService> logger,
        VssConnection vssConnection,
        AzureDevOpsOptions azureDevOpsOptions) : base(logger)
    {
        _vssConnection = vssConnection;
        _azureDevOpsOptions = azureDevOpsOptions;
    }

    protected override Task<List<Iss>> GetCurrentIssues(GitRepository gitRepository)
    {
        return Task.FromResult(new List<Iss>());
    }

    protected override async Task RaiseIssue(GitRepository gitRepository, PackageUpdateResult packageUpdateResult)
    {
        // TODO
    }

    protected override async Task CloseIssue(GitRepository gitRepository, Iss issue)
    {
        // TODO
    }

    protected override bool ShouldProcess(GitRepository gitRepository)
    {
        return false; // TODO: This hasn't been built yet
        return gitRepository.RepositoryType == RepositoryType.AzureDevOps;
    }
}