using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Models;

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

    public string CloneRepo(Repo repo)
    {
        var tempDirectory = _directoryService.CreateTemporaryDirectory();
        
        _logger.LogDebug("Creating Directory: {Directory}", tempDirectory);

        Repository.Clone(repo.GitUrl, Path.Combine(tempDirectory, repo.Name), new CloneOptions
        {
            CredentialsProvider = (url, fromUrl, types) => _gitCredentialsProvider.GetCredentials(repo.RepositoryType, types)
        });

        _logger.LogDebug("Cloned Repository {RepositoryName} into Directory {Directory}", repo.Name, tempDirectory);

        return tempDirectory;
    }
}