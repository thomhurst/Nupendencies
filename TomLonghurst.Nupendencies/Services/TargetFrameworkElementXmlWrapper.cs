using Microsoft.Build.Construction;

namespace TomLonghurst.Nupendencies.Services;

internal class TargetFrameworkElementXmlWrapper
{
    public string OldVersion { get; set; }
    public ProjectPropertyElement XmlElement { get; set; }
}