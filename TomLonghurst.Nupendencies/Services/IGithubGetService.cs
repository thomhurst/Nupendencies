using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IGithubGetService
{
    Task<IEnumerable<Repo>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string owner, string repo);
}