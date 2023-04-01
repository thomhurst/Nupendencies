namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record DependencyRemovalResult(bool IsSuccessful, string PackageName, ProjectPackage Package);