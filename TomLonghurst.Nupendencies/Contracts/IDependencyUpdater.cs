using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IDependencyUpdater
{
    Task<IList<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository);
}