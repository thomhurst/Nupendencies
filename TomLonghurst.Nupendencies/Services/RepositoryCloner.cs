using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies.Services;

public class RepositoryCloner : IRepositoryCloner
{
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<RepositoryCloner> _logger;

    public RepositoryCloner(IDirectoryService directoryService,
        ILogger<RepositoryCloner> logger)
    {
        _directoryService = directoryService;
        _logger = logger;
    }

    public string CloneRepository(GitRepository gitRepository)
    {
        var tempDirectory = _directoryService.CreateTemporaryDirectory();
        
        _logger.LogDebug("Creating Directory: {Directory}", tempDirectory);

        Repository.Clone(gitRepository.GitUrl, Path.Combine(tempDirectory, gitRepository.Name), new CloneOptions
        {
            FetchOptions =
            {
                CredentialsProvider = (_, _, _) => gitRepository.Credentials
            }
        });

        _logger.LogDebug("Cloned Repository {RepositoryName} into Directory {Directory}", gitRepository.Name, tempDirectory);

        return tempDirectory;
    }
}