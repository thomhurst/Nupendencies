using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Abstractions.Models;

public record ChildProject
{
    public required Project ParentProject { get; init; }
    public required ProjectItemElement ProjectReferenceTag { get; init; }
    public required Project Project { get; init; }
    
    public bool IsConditional
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ProjectReferenceTag.Condition))
            {
                return true;
            }

            if (ProjectReferenceTag.Parent == null)
            {
                return false;
            }
            
            return !string.IsNullOrWhiteSpace(ProjectReferenceTag.Parent.Condition);
        }
    }

    private ProjectElementContainer? _tagParent;
    private ProjectElement? _tagLastSibling;
    private ProjectElement? _tagNextSibling;
    public void RemoveReferenceTag()
    {
        _tagParent = ProjectReferenceTag.Parent;
        _tagLastSibling = ProjectReferenceTag.PreviousSibling;
        _tagNextSibling = ProjectReferenceTag.NextSibling;
        
        ProjectReferenceTag.Parent?.RemoveChild(ProjectReferenceTag);
        ParentProject.Save();
    }
    
    public void UndoRemove()
    {
        if (_tagLastSibling != null)
        {
            try
            {
                _tagParent?.InsertAfterChild(ProjectReferenceTag, _tagLastSibling);
            }
            catch
            {
                _tagParent?.AppendChild(ProjectReferenceTag);
            }
        }
        else if (_tagNextSibling != null)
        {
            try
            {
                _tagParent?.InsertBeforeChild(ProjectReferenceTag, _tagNextSibling);
            }
            catch
            {
                _tagParent?.AppendChild(ProjectReferenceTag);
            }
        }
        else
        {
            _tagParent?.AppendChild(ProjectReferenceTag);
        }
        
        ParentProject.Save();
    }
    
    public void Tidy()
    {
        if (_tagParent?.Children.Any() == false)
        {
            _tagParent.Parent.RemoveChild(_tagParent);
            Project.Save();
        }
    }

    public virtual bool Equals(ChildProject? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ProjectReferenceTag.Location.LocationString.Equals(other.ProjectReferenceTag.Location.LocationString) && Project.Equals(other.Project);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProjectReferenceTag.Location.LocationString, Project);
    }
}