namespace TomLonghurst.Nupendencies.Services;

public interface IUnusedDependencyRemover
{
    IAsyncEnumerable<DependencyRemovalResult> TryDeleteUnusedPackages(CodeRepository repository);
}