using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record TargetFrameworks
{
    public Project Project { get; }
    public ProjectPropertyElement? TargetFrameworksTag { get; set; }

    public TargetFrameworks(Project project)
    {
        Project = project;
        TargetFrameworksTag = project.ProjectRootElement.Properties.FirstOrDefault(x => x.Name == "TargetFrameworks");
    }

    public string[] Values => TargetFrameworksTag?.Value?
        .Split(';')
        .Select(x => x.Trim())
        .ToArray() ?? Array.Empty<string>();

    public bool HasValues => Values.Any();

    public bool IsMultiTargeted => Values.Length > 1;

    public bool IsNetCore => Values.Any(NetCoreParser.IsNetCore);
}