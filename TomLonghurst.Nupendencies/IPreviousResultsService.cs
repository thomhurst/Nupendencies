namespace TomLonghurst.Nupendencies;

public interface IPreviousResultsService
{
    void WriteUnableToRemovePackageEntry(string packageName, string project);
    bool ShouldTryRemove(string packageName, string project);
}