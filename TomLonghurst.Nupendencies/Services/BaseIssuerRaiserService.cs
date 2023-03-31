using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Services;

public abstract class BaseIssuerRaiserService : IIssuerRaiserService
{
    private readonly ILogger _logger;
    private const string NupendencyIssueTitleSuffix = "## Nupendencies Automated Issue ##";

    public BaseIssuerRaiserService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task CreateIssues(UpdateReport updateReport, GitRepository gitRepository)
    {
        if (!ShouldProcess(gitRepository))
        {
            return;
        }

        var currentIssues = await GetCurrentIssues(gitRepository);
        
        var existingNupendencyIssues = gitRepository.Issues
            .Where(i => i.Title.EndsWith(NupendencyIssueTitleSuffix))
            .ToList();

        var packageUpdateResults = updateReport.UpdatedPackagesResults;
        
        foreach (var packageUpdateResultGrouped in packageUpdateResults
                     .Where(u => !u.UpdateBuiltSuccessfully)
                     .GroupBy(p => p.PackageName))
        {
            var packageUpdateResult = packageUpdateResultGrouped.First();
            var matchingIssueForExactVersion = existingNupendencyIssues.FirstOrDefault(i => i.Title.StartsWith(GetTitlePrefixForPackage(packageUpdateResult)));

            if (matchingIssueForExactVersion != null)
            {
                // Issue already open for this exact scenario. No need to open another.
                continue;
            }
            
            var matchingIssueForOtherVersion = existingNupendencyIssues.FirstOrDefault(i => i.Title.StartsWith($"'{packageUpdateResult.PackageName}'"));

            if (matchingIssueForOtherVersion != null)
            {
                // Issue already open for this another version. We should close it and raise a new one with the new version.
                await CloseIssue(gitRepository, matchingIssueForOtherVersion);
            }

            _logger.LogDebug("Raising Issue for Package {PackageName} on Repo {RepoName}", packageUpdateResult.PackageName, gitRepository.Name);
            await RaiseIssue(gitRepository, packageUpdateResult);
        }
        
        foreach (var packageUpdateResultGrouped in packageUpdateResults
                     .Where(u => u.UpdateBuiltSuccessfully))
        {
            var previousIssueForNowSuccessfulPackageUpdate = currentIssues.FirstOrDefault(x => x.Title.StartsWith($"'{packageUpdateResultGrouped.PackageName}'")
                                             && x.Title.EndsWith(
                                                 "Dependency Update Failed ## Nupendencies Automated Issue ##"));
            
            if (previousIssueForNowSuccessfulPackageUpdate != null)
            {
                await CloseIssue(gitRepository, previousIssueForNowSuccessfulPackageUpdate);
            }
        }
    }

    protected abstract Task<IList<Iss>> GetCurrentIssues(GitRepository gitRepository);

    protected abstract Task RaiseIssue(GitRepository gitRepository, PackageUpdateResult packageUpdateResult);

    protected abstract Task CloseIssue(GitRepository gitRepository, Iss issue);
    
    protected abstract bool ShouldProcess(GitRepository gitRepository);
}