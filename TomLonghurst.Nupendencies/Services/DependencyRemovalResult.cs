namespace TomLonghurst.Nupendencies.Services;

public record DependencyRemovalResult(bool IsSuccessful, string PackageName, ProjectPackage Package);