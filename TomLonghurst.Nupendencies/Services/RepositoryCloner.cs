using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;

namespace TomLonghurst.Nupendencies.Services;

public class RepositoryCloner : IRepositoryCloner
{
    private readonly IDirectoryService _directoryService;
    private readonly IGitCredentialsProvider _gitCredentialsProvider;
    private readonly ILogger<RepositoryCloner> _logger;

    public RepositoryCloner(IDirectoryService directoryService,
        IGitCredentialsProvider gitCredentialsProvider,
        ILogger<RepositoryCloner> logger)
    {
        _directoryService = directoryService;
        _gitCredentialsProvider = gitCredentialsProvider;
        _logger = logger;
    }

    public string CloneRepository(GitRepository gitRepository)
    {
        var tempDirectory = _directoryService.CreateTemporaryDirectory();
        
        _logger.LogDebug("Creating Directory: {Directory}", tempDirectory);

        LibGit2Sharp.Repository.Clone(gitRepository.GitUrl, Path.Combine(tempDirectory, gitRepository.Name), new CloneOptions
        {
            CredentialsProvider = (_, _, types) => _gitCredentialsProvider.GetCredentials(gitRepository.RepositoryType, types)
        });

        _logger.LogDebug("Cloned Repository {RepositoryName} into Directory {Directory}", gitRepository.Name, tempDirectory);

        return tempDirectory;
    }
}