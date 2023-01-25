using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public abstract class BaseIssuerRaiserService : IIssuerRaiserService
{
    private readonly ILogger _logger;
    private const string NupendencyIssueTitleSuffix = "## Nupendencies Automated Issue ##";

    public BaseIssuerRaiserService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task CreateIssues(IEnumerable<PackageUpdateResult> packageUpdateResults, Repo repo)
    {
        if (!ShouldProcess(repo))
        {
            return;
        }
        
        var existingNupendencyIssues = repo.Issues
            .Where(i => i.Title.EndsWith(NupendencyIssueTitleSuffix))
            .ToList();
        
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
                await CloseIssue(repo, matchingIssueForOtherVersion);
            }

            _logger.LogDebug("Raising Issue for Package {PackageName} on Repo {RepoName}", packageUpdateResult.PackageName, repo.Name);
            await RaiseIssue(repo, packageUpdateResult);
        }
    }

    protected string GenerateBody(PackageUpdateResult packageUpdateResult)
    {
        return
            $@"There was a build issue when trying to automatically update the package '{packageUpdateResult.PackageName}'
This might need manual intervention.

Details:
Package Name: {packageUpdateResult.PackageName}
    Old Version: {packageUpdateResult.OldPackageVersion}
    New Version: {packageUpdateResult.NewPackageVersion}
    Locations: 
{string.Join(Environment.NewLine, packageUpdateResult.FileLines.Distinct().Select(line => $"\t- {line}"))}

Please try the update manually and fix and build or compilation issues and submit a pull request.

This is to keep our dependencies up to date so we are running secure, patched code with the latest fixes, optimisations and features!

This was an automatic issue by the Nupendencies scanning tool written by Tom Longhurst.";
    }

    protected string GenerateTitle(PackageUpdateResult packageUpdateResult)
    {
        return $"{GetTitlePrefixForPackage(packageUpdateResult)} {NupendencyIssueTitleSuffix}";
    }

    protected string GetTitlePrefixForPackage(PackageUpdateResult packageUpdateResult)
    {
        return $"'{packageUpdateResult.PackageName}' {packageUpdateResult.OldPackageVersion} > {packageUpdateResult.NewPackageVersion} - Dependency Update Failed";
    }

    protected abstract Task RaiseIssue(Repo repo, PackageUpdateResult packageUpdateResult);

    protected abstract Task CloseIssue(Repo repo, Iss issue);
    
    protected abstract bool ShouldProcess(Repo repo);
}