using Microsoft.Build.Construction;
using Semver;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record ProjectPackage
{
    public ProjectPackage(ProjectItemElement packageReferenceTag)
    {
        PackageReferenceTag = packageReferenceTag;
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

    public bool IsConditional
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PackageReferenceTag.Condition))
            {
                return true;
            }

            if (PackageReferenceTag.Parent == null)
            {
                return false;
            }
            
            return !string.IsNullOrWhiteSpace(PackageReferenceTag.Parent.Condition);
        }
    }

    public void RollbackVersion()
    {
        CurrentVersion = OriginalVersion;
    }

    private ProjectElementContainer? _tagParent;
    private ProjectElement? _tagLastSibling;
    private ProjectElement? _tagNextSibling;
    public void Remove()
    {
        _tagParent = PackageReferenceTag.Parent;
        _tagLastSibling = PackageReferenceTag.PreviousSibling;
        _tagNextSibling = PackageReferenceTag.NextSibling;
        
        _tagParent?.RemoveChild(PackageReferenceTag);
        Project.Save();
    }

    public void UndoRemove()
    {
        if (_tagLastSibling != null)
        {
            try
            {
                _tagParent?.InsertAfterChild(PackageReferenceTag, _tagLastSibling);
            }
            catch
            {
                _tagParent?.AppendChild(PackageReferenceTag);
            }
        }
        else if (_tagNextSibling != null)
        {
            try
            {
                _tagParent?.InsertBeforeChild(PackageReferenceTag, _tagNextSibling);
            }
            catch
            {
                _tagParent?.AppendChild(PackageReferenceTag);
            }
        }
        else
        {
            _tagParent?.AppendChild(PackageReferenceTag);
        }
        
        Project.Save();
    }

    public void Tidy()
    {
        if (!_tagParent.Children.Any())
        {
            _tagParent.Parent.RemoveChild(_tagParent);
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
        public bool Equals(ProjectPackage? x, ProjectPackage? y)
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