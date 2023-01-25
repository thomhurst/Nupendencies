namespace TomLonghurst.Nupendencies;

public record ProjectTreeItem(string AbsoluteFilePath)
{
    public ISet<ProjectTreeItem> Parents { get; } = new HashSet<ProjectTreeItem>();

    public ISet<ProjectTreeItem> Children { get; } = new HashSet<ProjectTreeItem>();

    public void AddParent(ProjectTreeItem projectTreeItem)
    {
        if (projectTreeItem.AbsoluteFilePath != AbsoluteFilePath)
        {
            Parents.Add(projectTreeItem);
        }
    }
    
    public void AddChild(ProjectTreeItem projectTreeItem)
    {
        if (projectTreeItem.AbsoluteFilePath != AbsoluteFilePath)
        {
            Children.Add(projectTreeItem);
        }
    }

    public virtual bool Equals(ProjectTreeItem? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return AbsoluteFilePath == other.AbsoluteFilePath;
    }

    public override int GetHashCode()
    {
        return AbsoluteFilePath.GetHashCode();
    }
}