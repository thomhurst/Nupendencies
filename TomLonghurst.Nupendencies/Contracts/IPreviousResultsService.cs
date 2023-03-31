using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Contracts;

public interface IPreviousResultsService
{
    void WriteUnableToRemovePackageEntry(ProjectPackage package);
    bool ShouldTryRemove(ProjectPackage package);
}