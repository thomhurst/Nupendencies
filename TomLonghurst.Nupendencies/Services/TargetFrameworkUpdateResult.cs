namespace TomLonghurst.Nupendencies.Services;

public record TargetFrameworkUpdateResult(bool IsSuccessful, string OriginalVersion, string LatestVersion);