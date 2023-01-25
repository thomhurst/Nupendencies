namespace TomLonghurst.Nupendencies;

internal record PreviousProblem(DateTimeOffset DateTimeOffset, string PackageName, string Project, PreviousProblemReason PreviousProblemReason);