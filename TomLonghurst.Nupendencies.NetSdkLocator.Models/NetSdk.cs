using System;

namespace TomLonghurst.Nupendencies.NetSdkLocator.Models;

public record NetSdk
{
    public string Directory { get; set; }
    public string Name { get; set; }
    public Version Version { get; set; }
    public bool IsDotNetSdk { get; set; }
    public bool Is64Bit { get; set; }
}