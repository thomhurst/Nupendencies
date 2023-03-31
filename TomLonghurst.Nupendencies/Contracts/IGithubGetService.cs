using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IGithubGetService
{
    Task<IEnumerable<GitRepository>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string owner, string repo);
}