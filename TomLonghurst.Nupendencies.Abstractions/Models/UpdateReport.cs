namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record UpdateReport(
    TargetFrameworkUpdateResult TargetFrameworkUpdateResult,
    IList<PackageRemovalResult> UnusedRemovedPackageReferencesResults,
    IList<ProjectRemovalResult> UnusedRemovedProjectReferencesResults,
    IList<PackageUpdateResult> UpdatedPackagesResults
);