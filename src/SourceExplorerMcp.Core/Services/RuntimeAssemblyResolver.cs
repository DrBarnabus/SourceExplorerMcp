using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SourceExplorerMcp.Core.Services;

public sealed class RuntimeAssemblyResolver(
    ILogger<RuntimeAssemblyResolver> logger)
    : IRuntimeAssemblyResolver
{
    private readonly ILogger<RuntimeAssemblyResolver> _logger = logger;

    public Dictionary<string, List<string>> ResolveRuntimeAssemblyPaths(IReadOnlyList<string> binDirectories, HashSet<string> frameworkReferences)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (frameworkReferences.Count == 0)
            return result;

        string? dotnetRoot = ResolveDotnetRoot();
        if (dotnetRoot is null)
        {
            _logger.LogWarning("Could not resolve .NET runtime root directory");
            return result;
        }

        var frameworks = DiscoverFrameworksFromRuntimeConfigs(binDirectories);
        foreach (var (name, version) in frameworks)
        {
            if (!frameworkReferences.Contains(name))
                continue;

            if (result.ContainsKey(name))
                continue;

            string? runtimePath = ResolveRuntimePath(dotnetRoot, name, version);
            if (runtimePath is null)
                continue;

            try
            {
                string[] dllFiles = Directory.GetFiles(runtimePath, "*.dll", SearchOption.TopDirectoryOnly);
                result[name] = [..dllFiles];
                _logger.LogDebug("Resolved {Count} assemblies from {Framework} at {Path}", dllFiles.Length, name, runtimePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning runtime directory: {Path}", runtimePath);
            }
        }

        return result;
    }

    private List<(string Name, string Version)> DiscoverFrameworksFromRuntimeConfigs(IReadOnlyList<string> binDirectories)
    {
        var frameworks = new List<(string Name, string Version)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string binDirectory in binDirectories)
        {
            try
            {
                foreach (string configFile in Directory.GetFiles(binDirectory, "*.runtimeconfig.json", SearchOption.AllDirectories))
                {
                    var parsed = ParseRuntimeConfig(configFile);
                    foreach (var framework in parsed)
                    {
                        if (seen.Add(framework.Name))
                            frameworks.Add(framework);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error scanning for runtimeconfig.json in {Path}", binDirectory);
            }
        }

        return frameworks;
    }

    private List<(string Name, string Version)> ParseRuntimeConfig(string configPath)
    {
        var frameworks = new List<(string Name, string Version)>();

        try
        {
            string json = File.ReadAllText(configPath);
            var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions))
                return frameworks;

            if (runtimeOptions.TryGetProperty("framework", out var framework))
            {
                var parsed = ParseFrameworkElement(framework);
                if (parsed is not null)
                    frameworks.Add(parsed.Value);
            }

            if (runtimeOptions.TryGetProperty("frameworks", out var frameworksArray))
            {
                foreach (var element in frameworksArray.EnumerateArray())
                {
                    var parsed = ParseFrameworkElement(element);
                    if (parsed is not null)
                        frameworks.Add(parsed.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing runtimeconfig.json at {Path}", configPath);
        }

        return frameworks;
    }

    private static (string Name, string Version)? ParseFrameworkElement(JsonElement element)
    {
        if (!element.TryGetProperty("name", out var nameElement) ||
            !element.TryGetProperty("version", out var versionElement))
            return null;

        string? name = nameElement.GetString();
        string? version = versionElement.GetString();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            return null;

        return (name, version);
    }

    private string? ResolveRuntimePath(string dotnetRoot, string frameworkName, string requestedVersion)
    {
        string sharedPath = Path.Combine(dotnetRoot, "shared", frameworkName);
        if (!Directory.Exists(sharedPath))
        {
            _logger.LogDebug("Shared framework directory not found: {Path}", sharedPath);
            return null;
        }

        if (!Version.TryParse(requestedVersion, out var requested))
        {
            _logger.LogDebug("Could not parse requested version: {Version}", requestedVersion);
            return null;
        }

        string? bestMatch = null;
        Version? bestVersion = null;

        foreach (string versionDir in Directory.GetDirectories(sharedPath))
        {
            string dirName = Path.GetFileName(versionDir);
            if (!Version.TryParse(dirName, out var installed))
                continue;

            if (installed.Major != requested.Major || installed.Minor != requested.Minor)
                continue;

            if (bestVersion is null || installed > bestVersion)
            {
                bestVersion = installed;
                bestMatch = versionDir;
            }
        }

        if (bestMatch is null)
            _logger.LogDebug("No matching runtime found for {Framework} {Version}", frameworkName, requestedVersion);
        else
            _logger.LogDebug("Resolved {Framework} {Requested} to {Actual}", frameworkName, requestedVersion, bestVersion);

        return bestMatch;
    }

    private string? ResolveDotnetRoot()
    {
        string? envRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot))
            return envRoot;

        foreach (string candidate in GetDefaultDotnetRootCandidates())
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        _logger.LogDebug("Could not find dotnet root in any default location");
        return null;
    }

    private static IEnumerable<string> GetDefaultDotnetRootCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/usr/local/share/dotnet";
        }
        else
        {
            yield return "/usr/share/dotnet";
            yield return "/usr/lib/dotnet";
        }
    }
}
