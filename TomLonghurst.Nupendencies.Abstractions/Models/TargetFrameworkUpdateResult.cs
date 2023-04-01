namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record TargetFrameworkUpdateResult(bool IsSuccessful, string OriginalVersion, string LatestVersion);