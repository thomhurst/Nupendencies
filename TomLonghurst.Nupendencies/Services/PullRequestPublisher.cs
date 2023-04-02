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
        var packageRemovalResults = updateReport.UnusedRemovedPackageReferencesResults;
        var projectRemovalResults = updateReport.UnusedRemovedProjectReferencesResults;
        
        var successfulPackageUpdates = packageUpdateResults.Count(r => r.UpdateBuiltSuccessfully);
        var successfulUnusedPackageRemovals = packageRemovalResults.Count(r => r.IsSuccessful);
        var successfulUnusedProjectRemovals = projectRemovalResults.Count(r => r.IsSuccessful);

        if (successfulPackageUpdates == 0
            && successfulUnusedPackageRemovals == 0
            && updateReport.UnusedRemovedProjectReferencesResults.Count == 0
            && !updateReport.TargetFrameworkUpdateResult.IsSuccessful)
        {
            return;
        }

        var existingOpenPrs = (await gitProvider.GetOpenPullRequests(gitRepository)).ToList();

        var prTitle = GenerateTitle(successfulPackageUpdates, successfulUnusedPackageRemovals, successfulUnusedProjectRemovals, updateReport.TargetFrameworkUpdateResult);
        var prBody = GenerateBody(packageUpdateResults, packageRemovalResults, projectRemovalResults, updateReport.TargetFrameworkUpdateResult);

        var matchingPrWithSamePackageUpdates = MatchingPrWithSamePackageUpdates(gitRepository, existingOpenPrs, prTitle, prBody);
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
                Title = prTitle,
                Body = prBody,
                BaseBranch = gitRepository.MainBranch,
                HeadBranch = GetBranchName(branchName)
            });
    }

    private static GitPullRequest? MatchingPrWithSamePackageUpdates(GitRepository gitRepository, IEnumerable<GitPullRequest> existingOpenPrs, string prTitle, string prBody)
    {
        return existingOpenPrs.FirstOrDefault(pr => pr.Title == prTitle && pr.Body == prBody.Truncate(gitRepository.Provider.PullRequestBodyCharacterLimit));
    }

    private string GenerateTitle(int updateCount, 
        int successfulUnusedPackageRemovals,
        int successfullyUnusedProjectReferencesRemovals,
        TargetFrameworkUpdateResult targetFrameworkUpdateResult)
    {
        var stringBuilder = new StringBuilder();

        if (targetFrameworkUpdateResult.IsSuccessful)
        {
            stringBuilder.Append($"{targetFrameworkUpdateResult.LatestVersion} | ");
        }

        if (updateCount > 0)
        {
            stringBuilder.Append($"{updateCount} ↑ | ");
        }
        
        if (successfulUnusedPackageRemovals + successfullyUnusedProjectReferencesRemovals > 0)
        {
            stringBuilder.Append($"{successfulUnusedPackageRemovals + successfullyUnusedProjectReferencesRemovals} ␡ | ");
        }
        
        return stringBuilder.Append(NupendencyPrTitleSuffix).ToString();
    }

    private string GenerateBody(ICollection<PackageUpdateResult> packageUpdateResults,
        ICollection<PackageRemovalResult> packageRemovalResults,
        ICollection<ProjectRemovalResult> projectRemovalResults,
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

        if (packageRemovalResults.Any() || projectRemovalResults.Any())
        {
            stringBuilder.AppendLine($"{packageRemovalResults.Count + projectRemovalResults.Count} ␡ | ");
            foreach (var packageRemovalResult in packageRemovalResults.OrderBy(x => x.PackageName))
            {
                stringBuilder.AppendLine($" - {packageRemovalResult.PackageName} from {packageRemovalResult.Package.Project.Name}");
            }
            
            foreach (var projectRemovalResult in projectRemovalResults.OrderBy(x => x.ProjectName))
            {
                stringBuilder.AppendLine($" - {projectRemovalResult.ProjectName} from {projectRemovalResult.ProjectRemovedFrom.Name}");
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