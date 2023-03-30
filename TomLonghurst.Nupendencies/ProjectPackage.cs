using Microsoft.Build.Construction;
using Semver;
using TomLonghurst.Nupendencies.Services;

namespace TomLonghurst.Nupendencies;

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

    private SemVersion _currentVersion = null!;

    public required SemVersion CurrentVersion
    {
        get => _currentVersion;
        set
        {
            XmlVersionTag.Value = value.ToString();
            Project.Save();
            _currentVersion = value;
        }
    }

    public ProjectItemElement PackageReferenceTag { get; }
    public ProjectMetadataElement XmlVersionTag { get; }

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
}