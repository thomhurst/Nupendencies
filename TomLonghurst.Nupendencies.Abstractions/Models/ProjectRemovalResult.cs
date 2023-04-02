namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record ProjectRemovalResult(bool IsSuccessful, string ProjectName, Project ProjectRemovedFrom);