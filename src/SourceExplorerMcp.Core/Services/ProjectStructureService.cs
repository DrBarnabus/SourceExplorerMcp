using System.IO.Hashing;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public sealed class ProjectStructureService(ILogger<ProjectStructureService> logger) : IProjectStructureService
{
    private readonly ILogger<ProjectStructureService> _logger = logger;

    public ProjectStructure Discover(string normalisedBasePath)
    {
        var projectAssetsFiles = DiscoverProjectAssetsFiles(normalisedBasePath);
        var binDirectories = DiscoverBinDirectories(normalisedBasePath);
        long fingerprint = ComputeFingerprint(normalisedBasePath, projectAssetsFiles, binDirectories);

        _logger.LogDebug(
            "Discovered {AssetCount} project.assets.json files and {BinCount} bin directories in {Path}",
            projectAssetsFiles.Count, binDirectories.Count, normalisedBasePath);

        return new ProjectStructure(projectAssetsFiles, binDirectories, fingerprint);
    }

    private long ComputeFingerprint(string basePath, IReadOnlyList<string> projectAssetsFiles, IReadOnlyList<string> binDirectories)
    {
        var sentinelPaths = new List<string>(projectAssetsFiles);

        foreach (string binDir in binDirectories)
        {
            try
            {
                string[] depsFiles = Directory.GetFiles(binDir, "*.deps.json", SearchOption.AllDirectories);
                sentinelPaths.AddRange(depsFiles);
            }
            catch (DirectoryNotFoundException) { }
        }

        sentinelPaths.Sort(StringComparer.Ordinal);

        if (sentinelPaths.Count == 0)
        {
            _logger.LogDebug("No sentinel files found in {Path}, returning zero fingerprint", basePath);
            return 0;
        }

        var hash = new HashCode();
        foreach (string path in sentinelPaths)
        {
            hash.Add(path, StringComparer.Ordinal);
            hash.Add(unchecked((long)XxHash3.HashToUInt64(File.ReadAllBytes(path))));
        }

        long fingerprint = hash.ToHashCode();
        _logger.LogDebug("Computed fingerprint {Fingerprint} from {Count} sentinel files in {Path}",
            fingerprint, sentinelPaths.Count, basePath);

        return fingerprint;
    }

    private static List<string> DiscoverProjectAssetsFiles(string basePath)
    {
        try
        {
            return [..Directory.GetFiles(basePath, "project.assets.json", SearchOption.AllDirectories)];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    private static List<string> DiscoverBinDirectories(string basePath)
    {
        var directories = new List<string>();

        try
        {
            directories.AddRange(Directory.GetDirectories(basePath, "bin", SearchOption.AllDirectories));
        }
        catch (DirectoryNotFoundException) { }

        if (Path.GetFileName(basePath).Equals("bin", StringComparison.OrdinalIgnoreCase))
            directories.Add(basePath);

        return directories;
    }
}
