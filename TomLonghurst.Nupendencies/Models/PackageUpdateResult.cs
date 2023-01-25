namespace TomLonghurst.Nupendencies.Models
{
    public record PackageUpdateResult
    {
        public string PackageName { get; set; }
        public string OldPackageVersion { get; set; }
        public string NewPackageVersion { get; set; }
        public bool UpdateBuiltSuccessfully { get; set; }

        public List<string> FileLines { get; set; } = new();
        public bool PackageDowngradeDetected { get; set; }

        public string GetBindingRedirectVersionString()
        {
            var version = new Version(UpdateBuiltSuccessfully ? NewPackageVersion : OldPackageVersion);
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}