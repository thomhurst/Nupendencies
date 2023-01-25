using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class NupendencyUpdater : INupendencyUpdater
{
    private readonly IGithubGetService _githubGetService;
    private readonly IDevOpsGetService _devOpsGetService;
    private readonly IRepositoryProcessorService _repositoryProcessorService;
    private readonly NupendenciesOptions _nupendenciesOptions;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<NupendencyUpdater> _logger;

    public NupendencyUpdater(IGithubGetService githubGetService,
        IDevOpsGetService devOpsGetService,
        IRepositoryProcessorService repositoryProcessorService,
        NupendenciesOptions nupendenciesOptions,
        IDirectoryService directoryService,
        ILogger<NupendencyUpdater> logger)
    {
        _githubGetService = githubGetService;
        _devOpsGetService = devOpsGetService;
        _repositoryProcessorService = repositoryProcessorService;
        _nupendenciesOptions = nupendenciesOptions;
        _directoryService = directoryService;
        _logger = logger;
    }

    public async Task Start()
    {
        _directoryService.TryCleanup();

        var gitHubRepositoriesTask = _githubGetService.GetRepositories();
        var devOpsRepositoriesTask = _devOpsGetService.GetRepositories();

        var allRepos = await Task.WhenAll(gitHubRepositoriesTask, devOpsRepositoriesTask);

        var repositoriesToInclude = allRepos.SelectMany(r => r)
            .Where(ShouldScanRepository);

        foreach (var repository in repositoriesToInclude)
        {
            await Process(repository);
        }

    }

    private async Task Process(Repo repository)
    {
        try
        {
            _logger.LogInformation("Processing Repository {RepositoryName}", repository.Name);
            await _repositoryProcessorService.Process(repository);
        }
        finally
        {
            _directoryService.TryCleanup();
        }
    }

    private bool ShouldScanRepository(Repo repo)
    {
        if (!_nupendenciesOptions.RepositoriesToScan.Any())
        {
            return true;
        }

        return _nupendenciesOptions.RepositoriesToScan.Any(func => func.Invoke(repo));
    }
}