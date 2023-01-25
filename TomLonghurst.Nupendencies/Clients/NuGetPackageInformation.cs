using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace TomLonghurst.Nupendencies.Clients;

public class NuGetPackageInformation
{
    public string PackageName { get; set; }
    public NuGetVersion Version { get; set; }
    public List<PackageDependency> Dependencies { get; set; }
}