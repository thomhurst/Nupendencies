namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record PackageRemovalResult(bool IsSuccessful, string PackageName, ProjectPackage Package);