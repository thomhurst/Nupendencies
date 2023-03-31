using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Contracts;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class GithubGetService : IGithubGetService
{
    private readonly IGithubGraphQlClientProvider _githubGraphQlClientProvider;
    private readonly NupendenciesOptions _nupendenciesOptions;

    public GithubGetService(IGithubGraphQlClientProvider githubGraphQlClientProvider, NupendenciesOptions nupendenciesOptions)
    {
        _githubGraphQlClientProvider = githubGraphQlClientProvider;
        _nupendenciesOptions = nupendenciesOptions;
    }

    public async Task<IEnumerable<GitRepository>> GetRepositories()
    {
        var query = new Query()
            .Organization(_nupendenciesOptions.GithubOptions.Organization)
            .Team(_nupendenciesOptions.GithubOptions.Team)
            .Repositories()
            .AllPages()
            .Select(r => new GitRepository(RepositoryType.Github)
            {
                Owner = r.Owner.Login,
                Name = r.Name,
                Id = r.Id.Value,
                IsDisabled = r.IsDisabled || r.IsArchived,
                GitUrl = r.SshUrl,
                MainBranch = r.DefaultBranchRef.Name,
                Issues = r.Issues(null, null, null, null, null, null, new IssueOrder
                        {
                            Direction = OrderDirection.Desc,
                            Field = IssueOrderField.UpdatedAt
                        },
                        new List<IssueState> { IssueState.Open })
                    .AllPages()
                    .Select(i => new Iss
                    {
                        IssueNumber = i.Number,
                        Id = i.Id.Value,
                        Title = i.Title,
                        Author = i.Author.Login,
                        Created = i.CreatedAt,
                        Updated = i.UpdatedAt,
                        IsClosed = i.Closed,
                    })
                    .ToList()
            }).Compile();

        return await _githubGraphQlClientProvider.GithubGraphQlClient.Run(query);
    }
    
    public async Task<IEnumerable<Pr>> GetOpenPullRequests(string owner, string repo)
    {
        var query = new Query()
            .Repository(repo, owner, true)
            .PullRequests(states: new List<PullRequestState> { PullRequestState.Open })
            .AllPages()
            .Select(p => new Pr
            {
                Number = p.Number,
                Id = p.Id.Value,
                Title = p.Title,
                Body = p.Body,
                HasConflicts = p.Mergeable == MergeableState.Conflicting
            }).Compile();

        return await _githubGraphQlClientProvider.GithubGraphQlClient.Run(query);
    }
}