using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public sealed partial class AssemblyDiscoveryService(
    ILogger<AssemblyDiscoveryService> logger,
    IProjectAssetsParser projectAssetsParser,
    IAssemblyMetadataExtractor assemblyMetadataExtractor,
    IMemoryCache cache)
    : IAssemblyDiscoveryService
{
    [GeneratedRegex(@"[/\\](net\d+\.\d+|netstandard\d+\.\d+|netcoreapp\d+\.\d+)[/\\]")]
    private static partial Regex TargetFrameworkRegex { get; }

    private const string CacheKeyPrefix = "assemblies:";

    private readonly ILogger<AssemblyDiscoveryService> _logger = logger;
    private readonly IProjectAssetsParser _projectAssetsParser = projectAssetsParser;
    private readonly IAssemblyMetadataExtractor _assemblyMetadataExtractor = assemblyMetadataExtractor;
    private readonly IMemoryCache _cache = cache;

    public async Task<List<AssemblyInfo>> DiscoverAssembliesAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        if (!Directory.Exists(basePath))
            throw new DirectoryNotFoundException($"Directory not found: {basePath}");

        string normalisedPath = Path.GetFullPath(basePath);
        string cacheKey = GetCacheKey(normalisedPath);

        if (_cache.TryGetValue<List<AssemblyInfo>>(cacheKey, out var cachedAssemblies) && cachedAssemblies is not null)
        {
            _logger.LogDebug("Retrieved {Count} assemblies from cache for {Path}",  cachedAssemblies.Count, normalisedPath);
            return cachedAssemblies;
        }

        _logger.LogInformation("Discovering assemblies in {Path}", normalisedPath);

        var assemblies = await Task.Run(() => DiscoverAssembliesInternal(normalisedPath, cancellationToken), cancellationToken);

        _cache.Set(cacheKey, assemblies);

        _logger.LogInformation("Discovered {Count} assemblies for {Path}", assemblies.Count, normalisedPath);
        return assemblies;
    }

    private List<AssemblyInfo> DiscoverAssembliesInternal(string basePath, CancellationToken cancellationToken = default)
    {
        var assemblyInfos = new List<AssemblyInfo>();

        try
        {
            var searchPaths = new List<string>();
            string[] binDirectories = Directory.GetDirectories(basePath, "bin", SearchOption.AllDirectories);
            searchPaths.AddRange(binDirectories);

            if (Path.GetFileName(basePath).Equals("bin", StringComparison.OrdinalIgnoreCase))
                searchPaths.Add(basePath);

            _logger.LogDebug("Found {Count} bin directories to search", searchPaths.Count);

            var packageMappings = DiscoverProjectAssets(basePath);

            foreach (string searchPath in searchPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string[] dllFilePaths = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                    foreach (string dllFilePath in dllFilePaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!ShouldIncludeAssembly(dllFilePath))
                            continue;

                        var assemblyInfo = CreateAssemblyInfo(dllFilePath, packageMappings);
                        if (assemblyInfo != null)
                            assemblyInfos.Add(assemblyInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching directory: {Path}", searchPath);
                }
            }

            _logger.LogDebug("Discovered {Count} assemblies after filtering and metadata extraction", assemblyInfos.Count);
            return assemblyInfos
                .OrderBy(a => a.PackageName)
                .ThenBy(a => a.AssemblyName)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering assemblies in: {Path}", basePath);
            throw;
        }
    }

    private Dictionary<string, Package> DiscoverProjectAssets(string basePath)
    {
        var allPackageMappings = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);

        string[] projectAssetsFiles = Directory.GetFiles(basePath, "project.assets.json", SearchOption.AllDirectories);
        _logger.LogDebug("Found {Count} project.assets.json files", projectAssetsFiles.Length);

        foreach (string projectAssetsFile in projectAssetsFiles)
        {
            var mappings = _projectAssetsParser.ParseProjectAssets(projectAssetsFile);
            if (mappings is null)
                continue;

            foreach ((string key, var value) in mappings)
                allPackageMappings.TryAdd(key, value);
        }

        return allPackageMappings;
    }

    private AssemblyInfo? CreateAssemblyInfo(string assemblyPath, Dictionary<string, Package> packageMappings)
    {
        try
        {
            var fileInfo = new FileInfo(assemblyPath);
            string fileName = fileInfo.Name;
            string assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (!packageMappings.TryGetValue(fileName, out var package))
                throw new InvalidOperationException($"Unable to find package for {fileName} in package mappings");

            var metadata = _assemblyMetadataExtractor.ExtractMetadata(assemblyPath);

            return new AssemblyInfo(
                package.Name,
                metadata?.AssemblyName ?? assemblyNameWithoutExtension,
                metadata?.Version ?? package.Version,
                fileInfo.FullName,
                metadata?.PublicKeyToken,
                ExtractTargetFramework(assemblyPath),
                fileInfo.LastWriteTimeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating assembly info for {Path}", assemblyPath);
            return null;
        }

        static string? ExtractTargetFramework(string assemblyPath)
        {
            var match = TargetFrameworkRegex.Match(assemblyPath);
            return match.Success ? match.Groups[1].Value : null;
        }
    }

    private bool ShouldIncludeAssembly(string assemblyPath)
    {
        if (assemblyPath.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase))
            return false;

        if (assemblyPath.Contains($"{Path.DirectorySeparatorChar}roslyn{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    private static string GetCacheKey(string normalisedPath) => $"{CacheKeyPrefix}{normalisedPath}";
}
