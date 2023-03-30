using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public interface ICodeRepositoryUpdater
{
    Task<IReadOnlyList<PackageUpdateResult>> UpdateRepository(CodeRepository repository);
}