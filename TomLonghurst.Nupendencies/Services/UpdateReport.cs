using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public record UpdateReport(
    TargetFrameworkUpdateResult TargetFrameworkUpdateResult,
    List<DependencyRemovalResult> UnusedRemovedPackagesResults,
    List<PackageUpdateResult> UpdatedPackagesResults
);