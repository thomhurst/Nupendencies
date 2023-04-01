namespace TomLonghurst.Nupendencies.Abstractions.Models;

public class NetCoreParser
{
    public static bool IsNetCore(string? frameworkVersion)
    {
        if (string.IsNullOrWhiteSpace(frameworkVersion))
        {
            return false;
        }

        if (frameworkVersion.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        
        if (frameworkVersion.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)
            && frameworkVersion.Contains('.'))
        {
            return true;
        }

        return false;
    }
}