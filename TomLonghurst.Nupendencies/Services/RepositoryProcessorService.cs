using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class RepositoryProcessorService : IRepositoryProcessorService
{
    private readonly IRepositoryCloner _repositoryCloner;
    private readonly ICodeRepositoryUpdater _codeRepositoryUpdater;
    private readonly IEnumerable<IIssuerRaiserService> _issuerRaiserServices;
    private readonly IEnumerable<IPullRequestPublisher> _pullRequestPublishers;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<RepositoryProcessorService> _logger;

    public RepositoryProcessorService(IRepositoryCloner repositoryCloner, 
        ICodeRepositoryUpdater codeRepositoryUpdater,
        IEnumerable<IIssuerRaiserService> issuerRaiserServices,
        IEnumerable<IPullRequestPublisher> pullRequestPublishers,
        IDirectoryService directoryService,
        ILogger<RepositoryProcessorService> logger)
    {
        _repositoryCloner = repositoryCloner;
        _codeRepositoryUpdater = codeRepositoryUpdater;
        _issuerRaiserServices = issuerRaiserServices;
        _pullRequestPublishers = pullRequestPublishers;
        _directoryService = directoryService;
        _logger = logger;
    }

    public async Task Process(GitRepository gitRepository)
    {
        try
        {
            var clonedLocation = _repositoryCloner.CloneRepository(gitRepository);

            var repository = new CodeRepository(clonedLocation);
            
            var updateReport = await _codeRepositoryUpdater.UpdateRepository(repository);
            
            await Task.WhenAll(_issuerRaiserServices.Select(s => s.CreateIssues(updateReport, gitRepository)));

            await Task.WhenAll(_pullRequestPublishers.Select(p => p.PublishPullRequest(clonedLocation, gitRepository, updateReport)));

            _directoryService.TryDeleteDirectory(clonedLocation);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating repository: {Repository}", gitRepository.Name);
        }
    }
}