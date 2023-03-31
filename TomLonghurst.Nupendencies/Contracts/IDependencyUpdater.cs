using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IDependencyUpdater
{
    Task<IList<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository);
}