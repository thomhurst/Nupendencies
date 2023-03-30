using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IDevOpsGetService
{
    Task<IEnumerable<GitRepository>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string repoId);
}