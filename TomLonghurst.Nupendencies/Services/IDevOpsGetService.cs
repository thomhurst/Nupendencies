using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IDevOpsGetService
{
    Task<IEnumerable<Repo>> GetRepositories();
    Task<IEnumerable<Pr>> GetOpenPullRequests(string repoId);
}