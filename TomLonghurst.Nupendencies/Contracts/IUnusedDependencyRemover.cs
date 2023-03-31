using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IUnusedDependencyRemover
{
    IAsyncEnumerable<DependencyRemovalResult> TryDeleteUnusedPackages(CodeRepository repository);
}