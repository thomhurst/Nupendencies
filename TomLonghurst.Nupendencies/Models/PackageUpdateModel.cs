using Semver;

namespace TomLonghurst.Nupendencies.Models;

public record PackageUpdateModel(string PackageName, SemVersion PreviousVersion, SemVersion NewVersion);