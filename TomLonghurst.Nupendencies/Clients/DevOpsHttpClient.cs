using System.Net.Http.Json;
using Polly;
using Polly.Extensions.Http;
using TomLonghurst.Nupendencies.Http;
using TomLonghurst.Nupendencies.Models.DevOps;

namespace TomLonghurst.Nupendencies.Clients;

public class DevOpsHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly NupendenciesOptions _nupendenciesOptions;

    public DevOpsHttpClient(HttpClient httpClient, NupendenciesOptions nupendenciesOptions)
    {
        _httpClient = httpClient;
        _nupendenciesOptions = nupendenciesOptions;
    }

    public async Task<List<DevOpsPullRequest>> GetPullRequestsForRepository(string repoId)
    {
        var response = await Get<DevOpsPullRequestsResponse>($"git/pullrequests?searchCriteria.includeLinks=false&searchCriteria.repositoryId={repoId}&$top={100}&api-version=7.1-preview.1");
        
        return response.PullRequests
            .Where(x => x.Status == "active")
            .ToList();
    }

    public async Task<List<DevOpsGitRepository>> GetGitRepositories()
    {
        var response = await Get<DevOpsGitRepositoryResponse>("git/repositories?api-version=7.1-preview.1");
        
        return response.Repositories
            .Where(x => !x.IsDisabled)
            .ToList();
    }

    public async Task CreateIssue(string repoId, string title, string body)
    {
        // TODO throw new NotImplementedException();
    }

    public async Task CloseIssue(string repoId, int issueNumber)
    {
        // TODO throw new NotImplementedException();
    }

    public async Task<HttpResponseMessage> CreatePullRequest(string repoId, string title, string body, string branchName,
        string repoMainBranch)
    {
        var response = await Post($"git/repositories/{repoId}/pullrequests?api-version=7.1-preview.1",
            new DevOpsCreatePullRequestModel 
            {
                SourceBranch = $"refs/heads/{branchName}",
                TargetBranch = repoMainBranch,
                Title = title,
                Body = body,
                WorkItemRefs = _nupendenciesOptions.AzureDevOpsOptions.WorkItemIds?.Select(x => new WorkItemRef { Id = x })?.ToArray()
            }
        );

        return response;
    }

    public async Task<HttpResponseMessage> ClosePr(string repoId, int prNumber)
    {
        var response = await Patch($"git/repositories/{repoId}/pullrequests/{prNumber}?api-version=7.1-preview.1",
            new DevOpsUpdatePrModel
            {
                Status = "abandoned"
            }
        );

        return response;
    }

    private async Task GetUserStories()
    {
        
    }

    public async Task<T?> Get<T>(string path)
    {
        var response = await 
            HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i * 2))
                .ExecuteAsync(() => _httpClient.GetAsync(path));

        return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>();

        //return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>();
    }

    public async Task<HttpResponseMessage> Post<T>(string path, T obj)
    {
        return await 
            HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i * 2))
                .ExecuteAsync(() => _httpClient.PostAsync(path, new JsonHttpContent(obj)));
    }
    
    public async Task<HttpResponseMessage> Patch<T>(string path, T obj)
    {
        return await 
            HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i * 2))
                .ExecuteAsync(() => _httpClient.PatchAsync(path,  new JsonHttpContent(obj)));
    }
}
