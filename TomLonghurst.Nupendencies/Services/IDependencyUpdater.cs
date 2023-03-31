using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IDependencyUpdater
{
    Task<IList<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository);
}