using Octokit.GraphQL;
using TomLonghurst.Nupendencies.Abstractions;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public class GitHubIssuerService : IGitHubIssuerService
{
    private readonly GitHubHttpClient _githubHttpClient;
    private readonly IGitHubGraphQlClientProvider _graphQlClientProvider;

    public GitHubIssuerService(GitHubHttpClient githubHttpClient,
        IGitHubGraphQlClientProvider graphQlClientProvider)
    {
        _githubHttpClient = githubHttpClient;
        _graphQlClientProvider = graphQlClientProvider;
    }

    public async Task<IList<GitIssue>> GetCurrentIssues(GitRepository gitRepository)
    {
        var query = new Query()
            .Organization(gitRepository.Owner)
            .Repository(gitRepository.Name)
            .Issues(null, null, null, null, null, null, null, null)
            .AllPages()
            .Select(x => new GitIssue
            {
                IssueNumber = x.Number,
                Id = x.Id.Value,
                Author = x.Author.Login,
                Created = x.CreatedAt,
                Title = x.Title,
                LastUpdated = x.UpdatedAt,
                IsClosed = x.Closed
            });

        var issues = await _graphQlClientProvider.GitHubGraphQlClient.Run(query);

        return issues.ToList();
    }

    public async Task RaiseIssue(GitRepository gitRepository, PackageUpdateResult packageUpdateResult)
    {
        await _githubHttpClient.CreateIssue(gitRepository.Owner,
            gitRepository.Name,
            GenerateTitle(packageUpdateResult),
            GenerateBody(packageUpdateResult)
        );
    }

    public async Task CloseIssue(GitRepository gitRepository, GitIssue issue)
    {
        await _githubHttpClient.CloseIssue(gitRepository.Owner, gitRepository.Name, issue.IssueNumber);
    }

    private string GenerateBody(PackageUpdateResult packageUpdateResult)
    {
        return
            $@"There was a build issue when trying to automatically update the package '{packageUpdateResult.PackageName}'
This might need manual intervention.

Details:
Package Name: {packageUpdateResult.PackageName}
    Old Version: {packageUpdateResult.Packages.First().OriginalVersion}
    New Version: {packageUpdateResult.LatestVersionAttempted}
    Locations: 
{string.Join(Environment.NewLine, packageUpdateResult.FileLines.Distinct().Select(line => $"\t- {line}"))}

Please try the update manually and fix and build or compilation issues and submit a pull request.

This is to keep our dependencies up to date so we are running secure, patched code with the latest fixes, optimisations and features!

This was an automatic issue by the Nupendencies scanning tool written by Tom Longhurst.";
    }

    private string GenerateTitle(PackageUpdateResult packageUpdateResult)
    {
        return $"{GetTitlePrefixForPackage(packageUpdateResult)} {NupendencyConstants.NupendencyIssueTitleSuffix}";
    }

    private string GetTitlePrefixForPackage(PackageUpdateResult packageUpdateResult)
    {
        return $"'{packageUpdateResult.PackageName}' {packageUpdateResult.Packages.First().OriginalVersion} > {packageUpdateResult.LatestVersionAttempted} - Dependency Update Failed";
    }
}