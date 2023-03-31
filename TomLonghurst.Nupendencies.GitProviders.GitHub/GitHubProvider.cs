﻿using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using TomLonghurst.Nupendencies.Abstractions.Contracts;
using TomLonghurst.Nupendencies.Abstractions.Models;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Clients;
using TomLonghurst.Nupendencies.GitProviders.GitHub.Options;

namespace TomLonghurst.Nupendencies.GitProviders.GitHub;

public class GitHubProvider : IGitProvider
{
    private readonly IGitHubGraphQlClientProvider _githubGraphQlClientProvider;
    private readonly IGitHubIssuerService _gitHubIssuerService;
    private readonly IGitHubPullRequestService _gitHubPullRequestService;
    private readonly GitHubOptions _githubOptions;

    public GitHubProvider(IGitHubGraphQlClientProvider githubGraphQlClientProvider,
        IGitHubIssuerService gitHubIssuerService,
        IGitHubPullRequestService gitHubPullRequestService,
        GitHubOptions githubOptions)
    {
        _githubGraphQlClientProvider = githubGraphQlClientProvider;
        _gitHubIssuerService = gitHubIssuerService;
        _gitHubPullRequestService = gitHubPullRequestService;
        _githubOptions = githubOptions;
    }

    public async Task<IEnumerable<GitRepository>> GetRepositories()
    {
        var query = new Query()
            .Organization(_githubOptions.Organization)
            .Team(_githubOptions.Team)
            .Repositories()
            .AllPages()
            .Select(r => new GitRepository
            {
                Owner = r.Owner.Login,
                Name = r.Name,
                Id = r.Id.Value,
                IsDisabled = r.IsDisabled || r.IsArchived,
                GitUrl = r.SshUrl.Replace("git@github.com:", "https://github.com/"),
                MainBranch = r.DefaultBranchRef.Name,
                Issues = r.Issues(null, null, null, null, null, null, new IssueOrder
                        {
                            Direction = OrderDirection.Desc,
                            Field = IssueOrderField.UpdatedAt
                        },
                        new List<IssueState> { IssueState.Open })
                    .AllPages()
                    .Select(i => new GitIssue
                    {
                        IssueNumber = i.Number,
                        Id = i.Id.Value,
                        Title = i.Title,
                        Author = i.Author.Login,
                        Created = i.CreatedAt,
                        LastUpdated = i.UpdatedAt,
                        IsClosed = i.Closed,
                    })
                    .ToList()
            }).Compile();

        return await _githubGraphQlClientProvider.GitHubGraphQlClient.Run(query);
    }

    public async Task<IEnumerable<GitPullRequest>> GetOpenPullRequests(GitRepository repository)
    {
        return await _gitHubPullRequestService.GetOpenPullRequests(repository);
    }

    public async Task CreatePullRequest(CreatePullRequestModel createPullRequestModel)
    {
        await _gitHubPullRequestService.CreatePullRequest(createPullRequestModel);
    }

    public async Task ClosePullRequest(GitRepository repository, GitPullRequest pullRequest)
    {
        await _gitHubPullRequestService.ClosePullRequest(repository, pullRequest);
    }

    public async Task<IEnumerable<GitIssue>> GetOpenIssues(GitRepository repository)
    {
        return await _gitHubIssuerService.GetCurrentIssues(repository);
    }

    public async Task CreateIssues(GitRepository repository, UpdateReport updateReport)
    {
        foreach (var failedPackageUpgrade in updateReport.UpdatedPackagesResults.Where(x => !x.UpdateBuiltSuccessfully))
        {
            await _gitHubIssuerService.RaiseIssue(repository, failedPackageUpgrade);
        }

        // TODO: Unused Dependencies and Target Framework
    }

    public async Task CloseIssue(GitRepository repository, GitIssue issue)
    {
        await _gitHubIssuerService.CloseIssue(repository, issue);
    }
}