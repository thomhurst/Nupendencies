namespace TomLonghurst.Nupendencies.Models;

public class Iss
{
    public int IssueNumber { get; set; }
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
    public bool IsClosed { get; set; }
}