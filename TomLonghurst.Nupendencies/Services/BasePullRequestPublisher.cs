using System.Text;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Extensions;
using TomLonghurst.Nupendencies.Options;
using GitRepository = TomLonghurst.Nupendencies.Models.GitRepository;

namespace TomLonghurst.Nupendencies.Services;

public abstract class BasePullRequestPublisher : IPullRequestPublisher
{
    protected readonly NupendenciesOptions NupendenciesOptions;
    private readonly IGitCredentialsProvider _gitCredentialsProvider;
    private readonly ILogger _logger;

    private const string NupendencyPrTitleSuffix = "## Nupendencies Automated PR ##";

    private const string PrBodyPrefix = "This is an automated pull request by the Nupendencies scanning tool";

    protected BasePullRequestPublisher(NupendenciesOptions nupendenciesOptions, IGitCredentialsProvider gitCredentialsProvider, ILogger logger)
    {
        NupendenciesOptions = nupendenciesOptions;
        _gitCredentialsProvider = gitCredentialsProvider;
        _logger = logger;
    }
    
    public async Task PublishPullRequest(string clonedLocation, GitRepository gitRepository,
        UpdateReport updateReport)
    {
        if (!ShouldProcess(gitRepository))
        {
            return;
        }

        var packageUpdateResults = updateReport.UpdatedPackagesResults;
        var deletionUpdateResults = updateReport.UnusedRemovedPackagesResults;
        
        var successfulUpdatesCount = packageUpdateResults.Count(r => r.UpdateBuiltSuccessfully)
            + deletionUpdateResults.Count(d => d.IsSuccessful);
        
        if (successfulUpdatesCount == 0)
        {
            return;
        }

        var existingOpenPrs = (await GetOpenPullRequests(gitRepository)).ToList();
        var prBody = GenerateBody(packageUpdateResults);

        var matchingPrWithSamePackageUpdates = MatchingPrWithSamePackageUpdates(gitRepository, existingOpenPrs, prBody);
        if (matchingPrWithSamePackageUpdates is { HasConflicts: false })
        {
            // There is an open PR identical to this one. Do nothing.
            _logger.LogDebug("PR with the same dependencies already exists on Repo {RepoName}", gitRepository.Name);
            return;
        }

        if (matchingPrWithSamePackageUpdates != null)
        {
            _logger.LogDebug("Closing Pull Request {Number} on Repo {RepoName} as it has conflicts", matchingPrWithSamePackageUpdates.Number, gitRepository.Name);
            await ClosePullRequest(gitRepository, matchingPrWithSamePackageUpdates);
        }

        var matchingPrWithOtherStaleUpdates = existingOpenPrs.FirstOrDefault(pr => pr.Body.StartsWith(PrBodyPrefix));
        if (matchingPrWithOtherStaleUpdates != null )
        {
            // There is an open PR that is stale. Close it and open a new one.
            _logger.LogDebug("Closing Pull Request {Number} on Repo {RepoName} as it is stale", matchingPrWithOtherStaleUpdates.Number, gitRepository.Name);
            await ClosePullRequest(gitRepository, matchingPrWithOtherStaleUpdates);
        }

        var branchName = $"feature/nupendencies-{DateTime.UtcNow.Ticks}";
        await PushChangesToRemoteBranch(new Repository(Path.Combine(clonedLocation, gitRepository.Name)), branchName, gitRepository.RepositoryType);

        _logger.LogDebug("Raising Pull Request with {SuccessfulUpdatesCount} updates on Repo {RepoName}", successfulUpdatesCount, gitRepository.Name);
        
        await CreatePullRequest(gitRepository, branchName, prBody, successfulUpdatesCount);
    }

    private static Pr? MatchingPrWithSamePackageUpdates(GitRepository gitRepository, IEnumerable<Pr> existingOpenPrs, string prBody)
    {
        if (gitRepository.RepositoryType == RepositoryType.AzureDevOps)
        {
            // Azure truncates to 400 characters wtf
            return existingOpenPrs.FirstOrDefault(pr => pr.Body == prBody.Truncate(400));
        }
        
        return existingOpenPrs.FirstOrDefault(pr => pr.Body == prBody);
    }

    protected string GenerateTitle(int updateCount)
    {
        var dateTime = DateTime.Now;
        
        return
            $"{updateCount} Dependency Updates - {dateTime.ToShortDateString()} {dateTime.ToShortTimeString()} {NupendencyPrTitleSuffix}";
    }

    protected string GenerateBody(IEnumerable<PackageUpdateResult> packageUpdateResults)
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

    private Task PushChangesToRemoteBranch(IRepository repo, string branchName, RepositoryType repoRepositoryType)
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
                CredentialsProvider = (_, _, types) => _gitCredentialsProvider.GetCredentials(repoRepositoryType, types)
            });
        }

        _logger.LogDebug("Finishing Push to Branch {BranchName}", branchName);
        
        return Task.CompletedTask;
    }

    private Signature GetSignature()
    {
        return new Signature(NupendenciesOptions.CommitterName, NupendenciesOptions.CommitterEmail, DateTimeOffset.UtcNow);
    }

    protected abstract Task<IEnumerable<Pr>> GetOpenPullRequests(GitRepository gitRepository);
    protected abstract Task ClosePullRequest(GitRepository gitRepository, Pr pr);
    protected abstract Task CreatePullRequest(GitRepository gitRepository, string branchName, string body, int updateCount);
    protected abstract bool ShouldProcess(GitRepository gitRepository);
}