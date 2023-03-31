﻿using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

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

    protected override Task<IList<Iss>> GetCurrentIssues(GitRepository gitRepository)
    {
        // Dev Ops doesn't really have an issues section. Raise a user story?
        return Task.FromResult<IList<Iss>>(new List<Iss>());
    }

    protected override async Task RaiseIssue(GitRepository gitRepository, PackageUpdateResult packageUpdateResult)
    {
        // TODO Dev Ops doesn't really have an issues section. Raise a user story?
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