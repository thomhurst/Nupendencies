using System.Collections.Immutable;
using TomLonghurst.Nupendencies.Abstractions.Models;

namespace TomLonghurst.Nupendencies.Abstractions.Extensions;

public static class ProjectExtensions
{
    public static ImmutableHashSet<Project> GetProjectsToBuild(this Project project)
    {
        return project.GetUppermostProjectsReferencingThisProject().ToImmutableHashSet();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this IEnumerable<Project> projects)
    {
        return projects
            .SelectMany(x => x.Repository.AllProjects)
            .SelectMany(GetProjectsToBuild)
            .ToImmutableHashSet();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this ProjectPackage projectPackage)
    {
        return projectPackage.Project.Repository.AllProjects.GetProjectsToBuild();
    }
    
    public static ImmutableHashSet<Project> GetProjectsToBuild(this IEnumerable<ProjectPackage> projectPackages)
    {
        return projectPackages
            .SelectMany(GetProjectsToBuild)
            .ToImmutableHashSet();
    }
}