using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Caching;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public sealed class AssemblyDiscoveryService(
    ILogger<AssemblyDiscoveryService> logger,
    IProjectAssetsParser projectAssetsParser,
    IAssemblyMetadataExtractor assemblyMetadataExtractor,
    IRuntimeAssemblyResolver runtimeAssemblyResolver,
    IMemoryCache cache,
    IProjectStructureService structureService)
    : IAssemblyDiscoveryService
{
    private const string CacheKeyPrefix = "assemblies:";

    private readonly ILogger<AssemblyDiscoveryService> _logger = logger;
    private readonly IProjectAssetsParser _projectAssetsParser = projectAssetsParser;
    private readonly IAssemblyMetadataExtractor _assemblyMetadataExtractor = assemblyMetadataExtractor;
    private readonly IRuntimeAssemblyResolver _runtimeAssemblyResolver = runtimeAssemblyResolver;
    private readonly IMemoryCache _cache = cache;
    private readonly IProjectStructureService _structureService = structureService;

    public async Task<DiscoveryResult> DiscoverAssembliesAsync(string basePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        if (!Directory.Exists(basePath))
            throw new DirectoryNotFoundException($"Directory not found: {basePath}");

        string normalisedPath = Path.GetFullPath(basePath);
        string cacheKey = GetCacheKey(normalisedPath);
        var structure = _structureService.Discover(normalisedPath);

        if (_cache.TryGetValid<DiscoveryResult>(cacheKey, structure.Fingerprint, out var cached))
        {
            _logger.LogDebug("Retrieved {Count} assemblies from cache for {Path}", cached.Assemblies.Count, normalisedPath);
            return cached;
        }

        _logger.LogInformation("Discovering assemblies in {Path}", normalisedPath);

        var result = await Task.Run(() => DiscoverAssembliesInternal(normalisedPath, structure, cancellationToken), cancellationToken);

        if (result.Assemblies.Count > 0)
            _cache.SetWithFingerprint(cacheKey, result, structure.Fingerprint);

        _logger.LogInformation("Discovered {Count} assemblies for {Path}", result.Assemblies.Count, normalisedPath);
        return result;
    }

    private DiscoveryResult DiscoverAssembliesInternal(string basePath, ProjectStructure structure, CancellationToken cancellationToken = default)
    {
        var assemblyInfos = new List<AssemblyInfo>();

        try
        {
            _logger.LogDebug("Found {Count} bin directories to search", structure.BinDirectories.Count);

            var (packageMappings, frameworkReferences) = ParseProjectAssets(structure.ProjectAssetsFiles);

            foreach (string searchPath in structure.BinDirectories)
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

                        var assemblyInfo = CreateAssemblyInfo(dllFilePath, packageMappings, frameworkReferences);
                        if (assemblyInfo != null)
                            assemblyInfos.Add(assemblyInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching directory: {Path}", searchPath);
                }
            }

            var runtimeAssemblies = _runtimeAssemblyResolver.ResolveRuntimeAssemblyPaths(structure.BinDirectories, frameworkReferences);
            foreach (var (frameworkName, dllPaths) in runtimeAssemblies)
            {
                foreach (string dllPath in dllPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var metadata = _assemblyMetadataExtractor.ExtractMetadata(dllPath);
                    if (metadata is null)
                        continue;

                    assemblyInfos.Add(new AssemblyInfo(
                        frameworkName,
                        metadata.AssemblyName,
                        metadata.Version,
                        dllPath));
                }
            }

            var deduplicated = assemblyInfos
                .OrderBy(a => a.PackageName)
                .ThenBy(a => a.AssemblyName)
                .DistinctBy(a => (a.PackageName, a.AssemblyName))
                .ToList();

            _logger.LogDebug(
                "Discovered {Total} assemblies, {Unique} unique after deduplication",
                assemblyInfos.Count, deduplicated.Count);

            if (deduplicated.Count > 0)
                return new DiscoveryResult(deduplicated, [], structure.Fingerprint);

            var diagnostics = new List<string>();
            if (structure.ProjectAssetsFiles.Count == 0)
                diagnostics.Add($"No project.assets.json found under '{basePath}'. Run 'dotnet restore' to generate the project dependency graph.");
            if (structure.BinDirectories.Count == 0)
                diagnostics.Add($"No bin/ directories found under '{basePath}'. Run 'dotnet build' to compile the project and its dependencies.");
            else if (structure.ProjectAssetsFiles.Count > 0)
                diagnostics.Add("No assemblies matched the project dependency graph. The project may need to be rebuilt.");

            return new DiscoveryResult(deduplicated, diagnostics, structure.Fingerprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering assemblies in: {Path}", basePath);
            throw;
        }
    }

    private (Dictionary<string, Package> PackageMappings, HashSet<string> FrameworkReferences) ParseProjectAssets(IReadOnlyList<string> projectAssetsFiles)
    {
        var allPackageMappings = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        var allFrameworkReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogDebug("Found {Count} project.assets.json files", projectAssetsFiles.Count);

        foreach (string projectAssetsFile in projectAssetsFiles)
        {
            var data = _projectAssetsParser.ParseProjectAssets(projectAssetsFile);
            if (data is null)
                continue;

            foreach ((string key, var value) in data.PackageMappings)
                allPackageMappings.TryAdd(key, value);

            allFrameworkReferences.UnionWith(data.FrameworkReferences);
        }

        return (allPackageMappings, allFrameworkReferences);
    }

    private AssemblyInfo? CreateAssemblyInfo(
        string assemblyPath,
        Dictionary<string, Package> packageMappings,
        HashSet<string> frameworkReferences)
    {
        try
        {
            var fileInfo = new FileInfo(assemblyPath);
            string fileName = fileInfo.Name;
            string assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (packageMappings.TryGetValue(fileName, out var package))
            {
                var metadata = _assemblyMetadataExtractor.ExtractMetadata(assemblyPath);
                return new AssemblyInfo(
                    package.Name,
                    metadata?.AssemblyName ?? assemblyNameWithoutExtension,
                    metadata?.Version ?? package.Version,
                    fileInfo.FullName);
            }

            if (frameworkReferences.Count > 0)
            {
                var metadata = _assemblyMetadataExtractor.ExtractMetadata(assemblyPath);
                if (metadata is null)
                    return null;

                string frameworkName = ResolveFrameworkReference(assemblyNameWithoutExtension, frameworkReferences);
                return new AssemblyInfo(
                    frameworkName,
                    metadata.AssemblyName,
                    metadata.Version,
                    fileInfo.FullName);
            }

            _logger.LogDebug("No package mapping or framework reference for {FileName}", fileName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating assembly info for {Path}", assemblyPath);
            return null;
        }
    }

    private static string ResolveFrameworkReference(string assemblyName, HashSet<string> frameworkReferences)
    {
        if (assemblyName.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
            && frameworkReferences.Contains("Microsoft.AspNetCore.App"))
            return "Microsoft.AspNetCore.App";

        if (IsWindowsDesktopAssembly(assemblyName)
            && frameworkReferences.Contains("Microsoft.WindowsDesktop.App"))
            return "Microsoft.WindowsDesktop.App";

        return frameworkReferences.Contains("Microsoft.NETCore.App")
            ? "Microsoft.NETCore.App"
            : frameworkReferences.First();
    }

    private static bool IsWindowsDesktopAssembly(string assemblyName) =>
        assemblyName.StartsWith("System.Windows.", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.StartsWith("Microsoft.Win32.", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Equals("WindowsBase", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Equals("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Equals("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Equals("WindowsFormsIntegration", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldIncludeAssembly(string assemblyPath)
    {
        if (assemblyPath.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase))
            return false;

        if (assemblyPath.Contains($"{Path.DirectorySeparatorChar}roslyn{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    private static string GetCacheKey(string normalisedPath) => $"{CacheKeyPrefix}{normalisedPath}";
}
