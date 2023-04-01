namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record UpdateReport(
    TargetFrameworkUpdateResult TargetFrameworkUpdateResult,
    IList<DependencyRemovalResult> UnusedRemovedPackagesResults,
    IList<PackageUpdateResult> UpdatedPackagesResults
);