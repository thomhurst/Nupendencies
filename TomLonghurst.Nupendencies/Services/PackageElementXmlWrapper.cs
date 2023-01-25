using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Services;

public class PackageElementXmlWrapper
{
    public ProjectMetadataElement XmlElement { get; set; }
    public string OldVersion { get; set; }
}