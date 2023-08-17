using Microsoft.Extensions.Logging;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class NupendencyUpdater : INupendencyUpdater
{
    private readonly IEnumerable<IGitProvider> _gitProviders;
    private readonly IRepositoryProcessorService _repositoryProcessorService;
    private readonly NupendenciesOptions _nupendenciesOptions;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<NupendencyUpdater> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NupendencyUpdater(IEnumerable<IGitProvider> gitProviders,
        IRepositoryProcessorService repositoryProcessorService,
        NupendenciesOptions nupendenciesOptions,
        IDirectoryService directoryService,
        ILogger<NupendencyUpdater> logger,
        IServiceProvider serviceProvider)
    {
        _gitProviders = gitProviders;
        _repositoryProcessorService = repositoryProcessorService;
        _nupendenciesOptions = nupendenciesOptions;
        _directoryService = directoryService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Start()
    {
        await _serviceProvider.InitializeAsync();
        
        _directoryService.TryCleanup();
        
        var repositories = await Task.WhenAll(_gitProviders.Select(x => x.GetRepositories()));

        var repositoriesToInclude = repositories
            .SelectMany(r => r)
            .Where(r => !r.IsDisabled)
            .Where(ShouldScanRepository);

        foreach (var repository in repositoriesToInclude)
        {
            await Process(repository);
        }

    }

    private async Task Process(GitRepository repository)
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

    private bool ShouldScanRepository(GitRepository repository)
    {
        if (_nupendenciesOptions.ShouldUpdateRepositoryPredicate == null)
        {
            return true;
        }

        return _nupendenciesOptions.ShouldUpdateRepositoryPredicate.Invoke(repository);
    }
}