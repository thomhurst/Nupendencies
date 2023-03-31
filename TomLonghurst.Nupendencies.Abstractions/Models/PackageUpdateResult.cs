namespace TomLonghurst.Nupendencies.Abstractions.Models
{
    public record PackageUpdateResult
    {
        public required string PackageName { get; init; }
        public required string LatestVersionAttempted { get; init; }
        public required HashSet<ProjectPackage> Packages { get; init; }
        public required bool UpdateBuiltSuccessfully { get; init; }

        public IEnumerable<string> FileLines => Packages
            .Select(x => x.Project.ProjectPath + $" | Line: {x.PackageReferenceTag.Location.Line}")
            .ToList();
        public required bool PackageDowngradeDetected { get; init; }
    }
}