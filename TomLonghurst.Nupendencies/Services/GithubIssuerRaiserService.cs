using Microsoft.Extensions.Logging;
using Octokit.GraphQL;
using TomLonghurst.Nupendencies.Clients;
using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Services;

public class GithubIssuerRaiserService : BaseIssuerRaiserService
{
    private readonly GithubHttpClient _githubHttpClient;
    private readonly IGithubGraphQlClientProvider _graphQlClientProvider;
    private readonly NupendenciesOptions _nupendenciesOptions;

    public GithubIssuerRaiserService(GithubHttpClient githubHttpClient,
        IGithubGraphQlClientProvider graphQlClientProvider,
        NupendenciesOptions nupendenciesOptions,
        ILogger<GithubIssuerRaiserService> logger) : base(logger)
    {
        _githubHttpClient = githubHttpClient;
        _graphQlClientProvider = graphQlClientProvider;
        _nupendenciesOptions = nupendenciesOptions;
    }

    protected override async Task<IList<Iss>> GetCurrentIssues(GitRepository gitRepository)
    {
        var query = new Query()
            .Organization(_nupendenciesOptions.GithubOptions.Organization)
            .Repository(gitRepository.Name)
            .Issues(null, null, null, null, null, null, null, null)
            .AllPages()
            .Select(x => new Iss
            {
                IssueNumber = x.Number,
                Id = x.Id.Value,
                Author = x.Author.Login,
                Created = x.CreatedAt,
                Title = x.Title,
                Updated = x.UpdatedAt,
                IsClosed = x.Closed
            });

        var issues = await _graphQlClientProvider.GithubGraphQlClient.Run(query);

        return issues.ToList();
    }

    protected override async Task RaiseIssue(GitRepository gitRepository, PackageUpdateResult packageUpdateResult)
    {
        await _githubHttpClient.CreateIssue(gitRepository.Owner,
            gitRepository.Name,
            GenerateTitle(packageUpdateResult),
            GenerateBody(packageUpdateResult)
        );
    }

    protected override async Task CloseIssue(GitRepository gitRepository, Iss issue)
    {
        await _githubHttpClient.CloseIssue(gitRepository.Owner, gitRepository.Name, issue.IssueNumber);
    }

    protected override bool ShouldProcess(GitRepository gitRepository)
    {
        return gitRepository.RepositoryType == RepositoryType.Github;
    }
}