using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace TomLonghurst.Nupendencies.Clients;

public class NuGetPackageInformation
{
    public required string PackageName { get; init; }
    public required NuGetVersion Version { get; init; }
    public required List<PackageDependency> Dependencies { get; init; }
}