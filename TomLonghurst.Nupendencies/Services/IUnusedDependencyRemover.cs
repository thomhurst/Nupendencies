using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IUnusedDependencyRemover
{
    IAsyncEnumerable<PackageUpdateResult> TryDeleteUnusedPackages(CodeRepository repository);
}