using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IUnusedDependencyRemover
{
    IAsyncEnumerable<PackageRemovalResult> TryDeleteRedundantPackageReferences(CodeRepository repository);

    IAsyncEnumerable<ProjectRemovalResult> TryDeleteRedundantProjectReferences(CodeRepository repository);
}