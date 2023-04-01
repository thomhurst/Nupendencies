namespace TomLonghurst.Nupendencies.Contracts;

public interface IDirectoryService
{
    string CreateTemporaryDirectory();
    bool TryDeleteDirectory(string path);
    void TryCleanup();
}