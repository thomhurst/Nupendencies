namespace TomLonghurst.Nupendencies.Models
{
    public record PackageUpdateResult
    {
        public required string PackageName { get; init; }
        public required string LatestVersionAttempted { get; init; }
        public required HashSet<ProjectPackage> Packages { get; init; }
        public required bool UpdateBuiltSuccessfully { get; set; }

        public List<string> FileLines => Packages
            .Select(x => x.Project.ProjectPath + $" | Line: {x.PackageReferenceTag.Location.Line}")
            .ToList();
        public required bool PackageDowngradeDetected { get; set; }
    }
}