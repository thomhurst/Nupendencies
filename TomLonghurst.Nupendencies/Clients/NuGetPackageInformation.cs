using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace TomLonghurst.Nupendencies.Clients;

public class NuGetPackageInformation
{
    public string PackageName { get; init; }
    public NuGetVersion Version { get; init; }
    public List<PackageDependency> Dependencies { get; init; }
}