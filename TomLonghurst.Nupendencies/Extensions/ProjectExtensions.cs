using System.Collections.Immutable;

namespace TomLonghurst.Nupendencies.Extensions;

public static class ProjectExtensions
{
    public static ImmutableHashSet<Project> GetProjectsToBuild(this Project project)
    {
        return project.GetUppermostProjectsReferencingThisProject().ToImmutableHashSet();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this IEnumerable<Project> projects)
    {
        return projects
            .SelectMany(GetProjectsToBuild)
            .ToImmutableHashSet();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this ProjectPackage projectPackage)
    {
        return projectPackage.Project.GetProjectsToBuild();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this IEnumerable<ProjectPackage> projectPackages)
    {
        return projectPackages
            .SelectMany(GetProjectsToBuild)
            .ToImmutableHashSet();
    }
}