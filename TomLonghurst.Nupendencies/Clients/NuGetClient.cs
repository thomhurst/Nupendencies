using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using TomLonghurst.Nupendencies.Options;

namespace TomLonghurst.Nupendencies.Clients;

public class NuGetClient
{
    private readonly IMemoryCache _memoryCache;
    private readonly NupendenciesOptions _nupendenciesOptions;
    private readonly ILogger<NuGetClient> _logger;
    
    private IList<PackageMetadataResource>? _nugetRepositories;

    public NuGetClient(IMemoryCache memoryCache, NupendenciesOptions nupendenciesOptions, ILogger<NuGetClient> logger)
    {
        _memoryCache = memoryCache;
        _nupendenciesOptions = nupendenciesOptions;
        _logger = logger;
    }

    private async Task<IList<PackageMetadataResource>> GetNuGetRepositories()
    {
        var baseNuget = await GetNugetRepository("https://api.nuget.org/v3/index.json");

        var tasks = _nupendenciesOptions?.PrivateNugetFeedOptions?
            .Select(x => GetNugetRepository(x.SourceUrl, x.Username, x.PatToken))
            .ToArray() ?? Array.Empty<Task<PackageMetadataResource>>();

        var privateFeeds = await Task.WhenAll(tasks);

        return new[] { baseNuget }.Concat(privateFeeds).ToList();
    }

    private async Task<PackageMetadataResource> GetNugetRepository(string indexUri, string? username = null, string? pat = null)
    {
     var packageSource = new PackageSource(indexUri);

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(pat))
        {
            packageSource.Credentials = new PackageSourceCredential(
                source: indexUri,
                username: username,
                passwordText: pat,
                isPasswordClearText: true,
                validAuthenticationTypesText: null);
        }

        var repository = Repository.Factory.GetCoreV3(packageSource);
        
        return await repository.GetResourceAsync<PackageMetadataResource>();
    }
    
    public async Task<NuGetPackageInformation[]> GetPackages(IEnumerable<string> packageNames)
    {
        var nuGetPackageInformations = await Task.WhenAll(packageNames.Select(x => GetPackage(x)));
        
        return nuGetPackageInformations
            .OfType<NuGetPackageInformation>()
            .ToArray();
    }

    public async Task<NuGetPackageInformation?> GetPackage(string packageName, string? version = null)
    {
        _nugetRepositories ??= await GetNuGetRepositories();

        if(_memoryCache.TryGetValue(packageName + version, out NuGetPackageInformation? nuGetPackageInformation))
        {
            _logger.LogDebug("Getting cached version of NuGet Package {PackageName}", packageName);
            return nuGetPackageInformation;
        }

        if (_memoryCache.TryGetValue(packageName + version, out NullInstance _))
        {
            return null;
        }

        var packageSearchMetadatas = new List<IPackageSearchMetadata>();
        
        _logger.LogDebug("Getting information for NuGet Package {PackageName}", packageName);
        
        foreach (var packageMetadataResource in _nugetRepositories)
        {
            try
            {
                var cacheForThisNugetRepo =
                    _memoryCache.GetOrCreate(packageMetadataResource, _ => new SourceCacheContext());
                
                var packageMetadatas = (await packageMetadataResource.GetMetadataAsync(packageName, includePrerelease: false, includeUnlisted: false,
                    cacheForThisNugetRepo, NullLogger.Instance, CancellationToken.None)).ToList();

                if (!packageMetadatas.Any())
                {
                    packageMetadatas = (await packageMetadataResource.GetMetadataAsync(packageName, includePrerelease: true, includeUnlisted: false,
                        cacheForThisNugetRepo, NullLogger.Instance, CancellationToken.None)).ToList();
                }
            
                if (!packageMetadatas.Any())
                {
                    continue;
                }

                var packagesWithVersions = packageMetadatas
                    .Where(x => x.Identity.HasVersion)
                    .Where(x => x.IsListed)
                    .Where(p => !p.Identity.Version.IsPrerelease)
                    .OrderByDescending(x => x.Identity.Version)
                    .ToList();

                if (!string.IsNullOrEmpty(version))
                {
                    var specifiedVersion = packagesWithVersions.FirstOrDefault(x => x.Identity.Version.OriginalVersion == version);
                    if (specifiedVersion is not null)
                    {
                        packageSearchMetadatas.Add(specifiedVersion);
                        break;
                    }
                }

                var maxVersion = packagesWithVersions.FirstOrDefault();

                if (maxVersion != null)
                {
                    packageSearchMetadatas.Add(maxVersion);
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        if (!packageSearchMetadatas.Any() && !string.IsNullOrEmpty(version))
        {
            _memoryCache.Set(packageName, new NullInstance());
            return null;
        }

        var packageSearchMetadata = packageSearchMetadatas.OrderByDescending(x => x.Identity.Version).First();

        var packageInformation = new NuGetPackageInformation
        {
            PackageName = packageName,
            Version = packageSearchMetadata.Identity.Version,
            Dependencies = packageSearchMetadata.DependencySets
                .SelectMany(x => x.Packages)
                .ToList()
        };

        _memoryCache.Set(packageName + version, packageInformation);
        
        return packageInformation;
    }
}