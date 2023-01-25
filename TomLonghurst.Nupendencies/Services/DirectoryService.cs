namespace TomLonghurst.Nupendencies.Services;

public class DirectoryService : IDirectoryService
{
    public string CreateTemporaryDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "Nupendencies", Path.GetRandomFileName());
        
        Directory.CreateDirectory(tempDirectory);
        
        return tempDirectory;
    }

    public bool TryDeleteDirectory(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);
            
            if (directory.Exists)
            {
                SetAttributesNormal(directory);
                directory.Delete(true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public void TryCleanup()
    {
        try
        {
            var nupendenciesDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Nupendencies"));

            if (!nupendenciesDirectory.Exists)
            {
                return;
            }

            foreach (var directory in nupendenciesDirectory.GetDirectories())
            {
                TryDeleteDirectory(directory.FullName);
            }
        }
        catch
        {
            // ignored
        }
    }

    private static void SetAttributesNormal(DirectoryInfo directory)
    {
        foreach (var subDirectory in directory.GetDirectories())
        {
            SetAttributesNormal(subDirectory);
        }

        foreach (var file in directory.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }
}