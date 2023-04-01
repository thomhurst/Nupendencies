using System.Text;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class PullRequestPublisher : IPullRequestPublisher
{
    private readonly NupendenciesOptions _nupendenciesOptions;
    private readonly ILogger<PullRequestPublisher> _logger;

    private const string NupendencyPrTitleSuffix = "## Nupendencies Automated PR ##";
    
    public PullRequestPublisher(NupendenciesOptions nupendenciesOptions, ILogger<PullRequestPublisher> logger)
    {
        _nupendenciesOptions = nupendenciesOptions;
        _logger = logger;
    }

    public async Task PublishPullRequest(string clonedLocation, GitRepository gitRepository,
        UpdateReport updateReport)
    {
        var gitProvider = gitRepository.Provider;
        
        var packageUpdateResults = updateReport.UpdatedPackagesResults;
        var deletionUpdateResults = updateReport.UnusedRemovedPackagesResults;
        
        var successfulPackageUpdates = packageUpdateResults.Count(r => r.UpdateBuiltSuccessfully);
        var successfulUnusedPackageRemovals = deletionUpdateResults.Count(r => r.IsSuccessful);
        
        if (successfulPackageUpdates == 0
            && successfulUnusedPackageRemovals == 0
            && !updateReport.TargetFrameworkUpdateResult.IsSuccessful)
        {
            return;
        }

        var existingOpenPrs = (await gitProvider.GetOpenPullRequests(gitRepository)).ToList();
        var prBody = GenerateBody(packageUpdateResults, deletionUpdateResults, updateReport.TargetFrameworkUpdateResult);

        var matchingPrWithSamePackageUpdates = MatchingPrWithSamePackageUpdates(gitRepository, existingOpenPrs, prBody);
        if (matchingPrWithSamePackageUpdates is { HasConflicts: false })
        {
            // There is an open PR identical to this one. Do nothing.
            _logger.LogInformation("A Pull Request with the same dependencies already exists on Repo {RepoName}", gitRepository.Name);
            return;
        }

        if (matchingPrWithSamePackageUpdates != null)
        {
            _logger.LogInformation("Closing Pull Request {Number} on Repo {RepoName} as it has conflicts", matchingPrWithSamePackageUpdates.Number, gitRepository.Name);
            await gitProvider.ClosePullRequest(gitRepository, matchingPrWithSamePackageUpdates);
        }

        var matchingPrWithOtherStaleUpdates = existingOpenPrs.FirstOrDefault(pr => pr.Title.Contains(NupendencyPrTitleSuffix));
        if (matchingPrWithOtherStaleUpdates != null )
        {
            // There is an open PR that is stale. Close it and open a new one.
            _logger.LogInformation("Closing Pull Request {Number} on Repo {RepoName} as it is stale", matchingPrWithOtherStaleUpdates.Number, gitRepository.Name);
            await gitProvider.ClosePullRequest(gitRepository, matchingPrWithOtherStaleUpdates);
        }

        var branchName = $"feature/nupendencies-{DateTime.UtcNow.Ticks}";
        await PushChangesToRemoteBranch(new Repository(Path.Combine(clonedLocation, gitRepository.Name)), branchName, gitRepository.Credentials);

        _logger.LogInformation("Raising Pull Request with {SuccessfulUpdatesCount} updates on Repo {RepoName}", successfulPackageUpdates, gitRepository.Name);
        
        await gitProvider.CreatePullRequest(
            new CreatePullRequestModel
            {
                UpdateReport = updateReport,
                Repository = gitRepository,
                Title = GenerateTitle(successfulPackageUpdates, successfulUnusedPackageRemovals, updateReport.TargetFrameworkUpdateResult),
                Body = prBody,
                BaseBranch = gitRepository.MainBranch,
                HeadBranch = GetBranchName(branchName)
            });
    }

    private static GitPullRequest? MatchingPrWithSamePackageUpdates(GitRepository gitRepository, IEnumerable<GitPullRequest> existingOpenPrs, string prBody)
    {
        return existingOpenPrs.FirstOrDefault(pr => pr.Body == prBody.Truncate(gitRepository.Provider.PullRequestBodyCharacterLimit));
    }

    private string GenerateTitle(int updateCount, int successfulUnusedPackageRemovals,
        TargetFrameworkUpdateResult targetFrameworkUpdateResult)
    {
        var dateTime = DateTime.Now;

        var stringBuilder = new StringBuilder();

        if (targetFrameworkUpdateResult.IsSuccessful)
        {
            stringBuilder.Append($"{targetFrameworkUpdateResult.LatestVersion} | ");
        }

        if (updateCount > 0)
        {
            stringBuilder.Append($"{updateCount} ↑ | ");
        }
        
        if (successfulUnusedPackageRemovals > 0)
        {
            stringBuilder.Append($"{successfulUnusedPackageRemovals} ␡ | ");
        }
        
        return stringBuilder.Append($"{dateTime.ToShortDateString()} {dateTime.ToShortTimeString()} {NupendencyPrTitleSuffix}").ToString();
    }

    private string GenerateBody(ICollection<PackageUpdateResult> packageUpdateResults,
        ICollection<DependencyRemovalResult> dependencyRemovalResults,
        TargetFrameworkUpdateResult targetFrameworkUpdateResult)
    {
        var stringBuilder = new StringBuilder();

        if (targetFrameworkUpdateResult.IsSuccessful)
        {
            stringBuilder.AppendLine(".NET Upgraded");
            stringBuilder.AppendLine($" - {targetFrameworkUpdateResult.OriginalVersion} > {targetFrameworkUpdateResult.LatestVersion}");
            stringBuilder.AppendLine();
        }

        if (packageUpdateResults.Any())
        {
            stringBuilder.AppendLine($"{packageUpdateResults.Count} ↑");
            foreach (var packageUpdateResult in packageUpdateResults.OrderBy(x => x.PackageName))
            {
                stringBuilder.AppendLine($" - {packageUpdateResult.PackageName} > {packageUpdateResult.Packages.First().OriginalVersion} > {packageUpdateResult.LatestVersionAttempted}");
            }
            stringBuilder.AppendLine();
        }

        
        
        if (dependencyRemovalResults.Any())
        {
            stringBuilder.AppendLine($"{dependencyRemovalResults.Count} ␡ | ");
            foreach (var dependencyRemovalResult in dependencyRemovalResults.OrderBy(x => x.PackageName))
            {
                stringBuilder.AppendLine($" - {dependencyRemovalResult.PackageName} > {dependencyRemovalResult.Package.Project.Name}");
            }
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    private string GetPackageList(IEnumerable<PackageUpdateResult> packageUpdateResults)
    {
        var sb = new StringBuilder();
        
        foreach (var packageUpdateResult in packageUpdateResults.Where(r => r.UpdateBuiltSuccessfully).OrderBy(r => r.PackageName))
        {
            sb.AppendLine($"{packageUpdateResult.PackageName} - {packageUpdateResult.Packages.First().OriginalVersion} > {packageUpdateResult.LatestVersionAttempted}");
        }

        return sb.ToString();
    }

    private Task PushChangesToRemoteBranch(IRepository repo, string branchName, Credentials credentials)
    {
        _logger.LogDebug("Starting Push to Branch {BranchName}", branchName);

        var branch = repo.CreateBranch(branchName);
        Commands.Checkout(repo, branch);
        
        var remote = repo.Network.Remotes["origin"];

        var result = repo.Branches.Update(branch,
            b => b.Remote = remote.Name,
            b => b.UpstreamBranch = branch.CanonicalName);
        
        var status = repo.RetrieveStatus();
        if (status.Modified.Any())
        {
            Commands.Stage(repo, status.Modified.Select(m => m.FilePath));
          
            var signature = GetSignature();
           
            repo.Commit("Nupendencies Automated Commit", signature, signature);
            
            repo.Network.Push(branch, new PushOptions
            {
                CredentialsProvider = (_, _, types) => credentials
            });
        }

        _logger.LogDebug("Finishing Push to Branch {BranchName}", branchName);
        
        return Task.CompletedTask;
    }

    private Signature GetSignature()
    {
        return new Signature(_nupendenciesOptions.CommitterName, _nupendenciesOptions.CommitterEmail, DateTimeOffset.UtcNow);
    }

    private static string GetBranchName(string branchName)
    {
        return branchName.StartsWith("refs/") ? branchName : $"refs/heads/{branchName}";
    }
}