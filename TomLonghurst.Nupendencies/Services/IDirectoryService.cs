namespace TomLonghurst.Nupendencies.Services;

public interface IDirectoryService
{
    string CreateTemporaryDirectory();
    bool TryDeleteDirectory(string path);
    void TryCleanup();
}