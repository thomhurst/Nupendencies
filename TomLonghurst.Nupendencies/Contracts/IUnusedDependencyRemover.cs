using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IUnusedDependencyRemover
{
    IAsyncEnumerable<DependencyRemovalResult> TryDeleteUnusedPackages(CodeRepository repository);
}