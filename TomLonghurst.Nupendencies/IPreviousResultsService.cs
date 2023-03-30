namespace TomLonghurst.Nupendencies;

public interface IPreviousResultsService
{
    void WriteUnableToRemovePackageEntry(ProjectPackage package);
    bool ShouldTryRemove(ProjectPackage package);
}