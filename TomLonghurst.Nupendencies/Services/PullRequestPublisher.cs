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

    private const string PrBodyPrefix = "This is an automated pull request by the Nupendencies scanning tool";

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
        
        var successfulUpdatesCount = packageUpdateResults.Count(r => r.UpdateBuiltSuccessfully)
            + deletionUpdateResults.Count(d => d.IsSuccessful);
        
        if (successfulUpdatesCount == 0)
        {
            return;
        }

        var existingOpenPrs = (await gitProvider.GetOpenPullRequests(gitRepository)).ToList();
        var prBody = GenerateBody(packageUpdateResults);

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

        var matchingPrWithOtherStaleUpdates = existingOpenPrs.FirstOrDefault(pr => pr.Body.StartsWith(PrBodyPrefix));
        if (matchingPrWithOtherStaleUpdates != null )
        {
            // There is an open PR that is stale. Close it and open a new one.
            _logger.LogInformation("Closing Pull Request {Number} on Repo {RepoName} as it is stale", matchingPrWithOtherStaleUpdates.Number, gitRepository.Name);
            await gitProvider.ClosePullRequest(gitRepository, matchingPrWithOtherStaleUpdates);
        }

        var branchName = $"feature/nupendencies-{DateTime.UtcNow.Ticks}";
        await PushChangesToRemoteBranch(new Repository(Path.Combine(clonedLocation, gitRepository.Name)), branchName, gitRepository.Credentials);

        _logger.LogInformation("Raising Pull Request with {SuccessfulUpdatesCount} updates on Repo {RepoName}", successfulUpdatesCount, gitRepository.Name);
        
        await gitProvider.CreatePullRequest(
            new CreatePullRequestModel()
            {
                UpdateReport = updateReport,
                Repository = gitRepository,
                Title = GenerateTitle(successfulUpdatesCount),
                Body = prBody,
                BaseBranch = gitRepository.MainBranch,
                HeadBranch = GetBranchName(branchName)
            });
    }

    private static GitPullRequest? MatchingPrWithSamePackageUpdates(GitRepository gitRepository, IEnumerable<GitPullRequest> existingOpenPrs, string prBody)
    {
        return existingOpenPrs.FirstOrDefault(pr => pr.Body == prBody.Truncate(gitRepository.Provider.PullRequestBodyCharacterLimit));
    }

    private string GenerateTitle(int updateCount)
    {
        var dateTime = DateTime.Now;
        
        return
            $"{updateCount} Dependency Updates - {dateTime.ToShortDateString()} {dateTime.ToShortTimeString()} {NupendencyPrTitleSuffix}";
    }

    private string GenerateBody(IEnumerable<PackageUpdateResult> packageUpdateResults)
    {
        return $@"{PrBodyPrefix}

The following packages have been updated:

{GetPackageList(packageUpdateResults)}

If the build does not succeed, please manually test the pull request and fix any issues.";
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

        repo.Branches.Update(branch,
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