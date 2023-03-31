using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record TargetFramework
{
    private string? _currentValue;
    public Project Project { get; }
    public ProjectPropertyElement? TargetFrameworkTag { get; }

    public TargetFramework(Project project)
    {
        Project = project;
        TargetFrameworkTag = project.ProjectRootElement.Properties.FirstOrDefault(x => x.Name == "TargetFramework");
        OriginalValue = TargetFrameworkTag?.Value;
        CurrentValue = OriginalValue;
    }

    public string? OriginalValue { get; }

    public string? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (OriginalValue != null)
            {
                TargetFrameworkTag!.Value = value;
                Project.Save();
                _currentValue = value;
            }
        }
    }

    public void Rollback()
    {
        CurrentValue = OriginalValue;
    }
    
    public bool HasValue => !string.IsNullOrWhiteSpace(OriginalValue);
    
    public bool IsNetCore => NetCoreParser.IsNetCore(OriginalValue);
}