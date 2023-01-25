using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public class RepositoryProcessorService : IRepositoryProcessorService
{
    private readonly IRepositoryCloner _repositoryCloner;
    private readonly ISolutionUpdater _solutionUpdater;
    private readonly IEnumerable<IIssuerRaiserService> _issuerRaiserServices;
    private readonly IEnumerable<IPullRequestPublisher> _pullRequestPublishers;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<RepositoryProcessorService> _logger;

    public RepositoryProcessorService(IRepositoryCloner repositoryCloner, 
        ISolutionUpdater solutionUpdater,
        IEnumerable<IIssuerRaiserService> issuerRaiserServices,
        IEnumerable<IPullRequestPublisher> pullRequestPublishers,
        IDirectoryService directoryService,
        ILogger<RepositoryProcessorService> logger)
    {
        _repositoryCloner = repositoryCloner;
        _solutionUpdater = solutionUpdater;
        _issuerRaiserServices = issuerRaiserServices;
        _pullRequestPublishers = pullRequestPublishers;
        _directoryService = directoryService;
        _logger = logger;
    }

    public async Task Process(Repo repo)
    {
        try
        {
            var clonedLocation = _repositoryCloner.CloneRepo(repo);

            var solutionFilePaths = GetSolutionFilePaths(clonedLocation);
            
            var updateResults = await _solutionUpdater.UpdateSolutions(solutionFilePaths);
            updateResults = EnumerableExtensions.DistinctBy(updateResults, x => x.PackageName)
                .OrderBy(x => x.PackageName)
                .ToList();
        
            await Task.WhenAll(_issuerRaiserServices.Select(s => s.CreateIssues(updateResults, repo)));

            await Task.WhenAll(_pullRequestPublishers.Select(p => p.PublishPullRequest(clonedLocation, repo, updateResults)));

            _directoryService.TryDeleteDirectory(clonedLocation);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating repository: {Repository}", repo.Name);
        }
    }

    private string[] GetSolutionFilePaths(string clonedLocation)
    {
        return Directory.GetFiles(clonedLocation, "*.sln", SearchOption.AllDirectories);
    }
}