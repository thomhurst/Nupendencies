using Microsoft.Build.Construction;
using Semver;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record ProjectPackage
{
    private readonly ProjectElementContainer _parent;
    private readonly ProjectElement _lastSibling;
    private readonly ProjectElement _nextSibling;
    
    public ProjectPackage(ProjectItemElement packageReferenceTag)
    {
        PackageReferenceTag = packageReferenceTag;
        _parent = packageReferenceTag.Parent;
        _lastSibling = packageReferenceTag.PreviousSibling;
        _nextSibling = packageReferenceTag.NextSibling;
        XmlVersionTag = packageReferenceTag.Metadata.First(m => m.Name == "Version");
    }
    public required string Name { get; init; }
    public required Project Project { get; init; }
    public required SemVersion OriginalVersion { get; init; }

    private SemVersion? _currentVersion;

    public SemVersion CurrentVersion
    {
        get => _currentVersion ?? OriginalVersion;
        set
        {
            XmlVersionTag.Value = value.ToString();
            Project.Save();
            _currentVersion = value;
        }
    }

    public ProjectItemElement PackageReferenceTag { get; }
    public ProjectMetadataElement XmlVersionTag { get; }

    public bool IsConditional => !string.IsNullOrWhiteSpace(PackageReferenceTag.Condition)
                                 || !string.IsNullOrWhiteSpace(PackageReferenceTag.Parent.Condition);

    public void RollbackVersion()
    {
        CurrentVersion = OriginalVersion;
    }

    public void Remove()
    {
        _parent.RemoveChild(PackageReferenceTag);
        Project.Save();
    }

    public void UndoRemove()
    {
        if (_lastSibling != null)
        {
            _parent.InsertAfterChild(PackageReferenceTag, _lastSibling);
        }
        else if (_nextSibling != null)
        {
            _parent.InsertBeforeChild(PackageReferenceTag, _nextSibling);
        }
        else
        {
            _parent.AppendChild(PackageReferenceTag);
        }
        
        Project.Save();
    }

    public void Tidy()
    {
        if (!_parent.Children.Any())
        {
            _parent.Parent.RemoveChild(_parent);
            Project.Save();
        }
    }

    public virtual bool Equals(ProjectPackage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Name != other.Name) return false;
        if (Project != other.Project) return false;
        return PackageReferenceTag.Location.Line.Equals(other.PackageReferenceTag.Location.Line);
    }

    public override int GetHashCode()
    {
        return (PackageReferenceTag.Location.File + PackageReferenceTag.Location.Line).GetHashCode();
    }

    private sealed class PackageReferenceTagEqualityComparer : IEqualityComparer<ProjectPackage>
    {
        public bool Equals(ProjectPackage x, ProjectPackage y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            if (x.Name != y.Name) return false;
            if (x.Project != y.Project) return false;
            return x.PackageReferenceTag.Location.Line.Equals(y.PackageReferenceTag.Location.Line);
        }

        public int GetHashCode(ProjectPackage obj)
        {
            return obj.PackageReferenceTag.GetHashCode();
        }
    }

    public static IEqualityComparer<ProjectPackage> Comparer { get; } = new PackageReferenceTagEqualityComparer();
}