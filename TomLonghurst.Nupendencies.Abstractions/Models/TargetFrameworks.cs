using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record TargetFrameworks(ProjectPropertyElement? TargetFrameworksTag)
{
    public string[] Values => TargetFrameworksTag?.Value?
        .Split(';')
        .Select(x => x.Trim())
        .ToArray() ?? Array.Empty<string>();

    public bool HasValues => Values.Any();

    public bool IsMultiTargeted => Values.Length > 1;

    public bool IsNetCore => Values.Any(NetCoreParser.IsNetCore);
}