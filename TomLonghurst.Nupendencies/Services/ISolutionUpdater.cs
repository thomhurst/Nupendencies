using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface ISolutionUpdater
{
    Task<IReadOnlyList<PackageUpdateResult>> UpdateSolutions(string[] solutionPaths);
}