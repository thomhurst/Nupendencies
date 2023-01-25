using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using TomLonghurst.Nupendencies.Http;
using TomLonghurst.Nupendencies.Models.Github;

namespace TomLonghurst.Nupendencies.Clients
{
    public class GithubHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<GithubHttpClient> _logger;

        public GithubHttpClient(HttpClient httpClient, ILogger<GithubHttpClient> logger)
        {
            _client = httpClient;
            _logger = logger;
        }
    
        public async Task<T?> Get<T>(string path)
        {
            var response = await 
                HttpPolicyExtensions.HandleTransientHttpError()
                    .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i * 2))
                    .ExecuteAsync(() => _client.GetAsync(path));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error Response: {Response}", await response.Content.ReadAsStringAsync());
            }

            return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>();
            
            //return await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>();
        }

        public async Task<HttpResponseMessage> CreateIssue(string owner, string repo, string title, string body)
        {
            var response = await _client.PostAsync($"repos/{owner}/{repo}/issues", new JsonHttpContent(new GithubCreateIssueModel
            {
                Owner = owner,
                Repo = repo,
                Title = title,
                Body = body
            }));
            
            return response;
        }
        
        public async Task<HttpResponseMessage> CloseIssue(string owner, string repo, int issueNumber)
        {
            var response = await _client.PostAsync($"repos/{owner}/{repo}/issues/{issueNumber}", new JsonHttpContent(new GithubUpdateIssueModel
            {
                Owner = owner,
                Repo = repo,
                IssueNumber = issueNumber,
                State = "closed"
            }));
            
            return response;
        }

        public async Task<HttpResponseMessage> CreatePullRequest(string owner, string repo, string title, string body, string head, string @base)
        {
            var response = await _client.PostAsync($"repos/{owner}/{repo}/pulls", new JsonHttpContent(new GithubCreatePullRequestModel
            {
                Owner = owner,
                Repo = repo,
                Title = title,
                Body = body,
                Head = head,
                Base = @base
            }));
            
            return response;
        }

        public async Task<HttpResponseMessage> ClosePr(string owner, string repo, int prNumber)
        {
            var response = await _client.PostAsync($"repos/{owner}/{repo}/pulls/{prNumber}", new JsonHttpContent(new GithubUpdatePrModel
            {
                Owner = owner,
                Repo = repo,
                PrNumber = prNumber,
                State = "closed"
            }));

            return response;
        }
    }
}