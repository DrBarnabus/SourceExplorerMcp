using System.Text.Json;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public sealed class ProjectAssetsParser(
    ILogger<ProjectAssetsParser> logger)
    : IProjectAssetsParser
{
    private readonly ILogger<ProjectAssetsParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Dictionary<string, Package>? ParseProjectAssets(string projectAssetsPath)
    {
        try
        {
            if (!File.Exists(projectAssetsPath))
            {
                _logger.LogDebug("project.assets.json not found at {Path}", projectAssetsPath);
                return null;
            }

            _logger.LogDebug("Parsing project.assets.json at {Path}", projectAssetsPath);

            string json = File.ReadAllText(projectAssetsPath);
            var jsonDocument = JsonDocument.Parse(json);

            var result = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);

            if (jsonDocument.RootElement.TryGetProperty("libraries", out var libraries))
            {
                foreach (var library in libraries.EnumerateObject())
                {
                    string libraryKey = library.Name;
                    if (libraryKey.Split('/') is not [{ } packageName, { } version])
                    {
                        _logger.LogTrace("Skipping invalid library key: {Key}", libraryKey);
                        continue;
                    }

                    if (!library.Value.TryGetProperty("type", out var typeElement) || typeElement.ToString() != "package")
                        continue;

                    var package = new Package(packageName, version, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    if (library.Value.TryGetProperty("files", out var files))
                    {
                        foreach (string filePath in files.EnumerateArray().Select(file => file.GetString()).OfType<string>())
                        {
                            if (!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string fileName = Path.GetFileName(filePath);
                            package.AssemblyFiles.Add(fileName);

                            result.TryAdd(fileName, package);
                        }
                    }
                }
            }

            _logger.LogDebug("Parsed {Count} assembly mappings from project.assets.json at {Path}", result.Count,
                projectAssetsPath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing project.assets.json at {Path}", projectAssetsPath);
            return null;
        }
    }
}
