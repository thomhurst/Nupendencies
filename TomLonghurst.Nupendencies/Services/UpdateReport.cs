using TomLonghurst.Nupendencies.Models;

namespace TomLonghurst.Nupendencies.Services;

public record UpdateReport(
    TargetFrameworkUpdateResult TargetFrameworkUpdateResult,
    IList<DependencyRemovalResult> UnusedRemovedPackagesResults,
    IList<PackageUpdateResult> UpdatedPackagesResults
);