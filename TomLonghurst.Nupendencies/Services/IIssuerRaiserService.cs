using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IIssuerRaiserService
{
    Task CreateIssues(IEnumerable<PackageUpdateResult> packageUpdateResults, Repo repo);
}