using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface IDependencyUpdater
{
    Task<List<PackageUpdateResult>> TryUpdatePackages(CodeRepository codeRepository);
}