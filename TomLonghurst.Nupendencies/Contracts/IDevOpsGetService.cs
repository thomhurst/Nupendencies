using TomLonghurst.Nupendencies.Models;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IDevOpsGetService
{
    Task<IEnumerable<GitRepository>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string repoId);
}