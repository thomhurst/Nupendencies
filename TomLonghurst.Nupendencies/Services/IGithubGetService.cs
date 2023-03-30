using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IGithubGetService
{
    Task<IEnumerable<GitRepository>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string owner, string repo);
}