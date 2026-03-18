using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class RuntimeAssemblyResolverTests
{
    private readonly RuntimeAssemblyResolver _sut = new(NullLogger<RuntimeAssemblyResolver>.Instance);

    [Fact]
    public void ResolveRuntimeAssemblyPaths_EmptyFrameworkReferences_ReturnsEmptyDictionary()
    {
        var binDirs = new List<string> { TestHelpers.FindRepoRoot() };
        var frameworkRefs = new HashSet<string>();

        var result = _sut.ResolveRuntimeAssemblyPaths(binDirs, frameworkRefs);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_ResolvesNETCoreAppAssemblies()
    {
        var binDirs = FindBinDirectories();
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths(binDirs, frameworkRefs);

        Assert.True(result.ContainsKey("Microsoft.NETCore.App"));
        Assert.NotEmpty(result["Microsoft.NETCore.App"]);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_IncludesSystemNetHttpDll()
    {
        var binDirs = FindBinDirectories();
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths(binDirs, frameworkRefs);

        Assert.True(result.ContainsKey("Microsoft.NETCore.App"));
        Assert.Contains(result["Microsoft.NETCore.App"], p => Path.GetFileName(p) == "System.Net.Http.dll");
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_ExcludesFrameworksNotInReferenceSet()
    {
        var binDirs = FindBinDirectories();
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths(binDirs, frameworkRefs);

        Assert.DoesNotContain("Microsoft.AspNetCore.App", result.Keys);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_NonExistentFramework_ReturnsEmptyForThatFramework()
    {
        var binDirs = FindBinDirectories();
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NonExistent.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths(binDirs, frameworkRefs);

        Assert.DoesNotContain("Microsoft.NonExistent.App", result.Keys);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_EmptyBinDirectories_ReturnsEmptyDictionary()
    {
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths([], frameworkRefs);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_RuntimeConfigWithSingleFramework_Parsed()
    {
        string configPath = TestHelpers.WriteTempFile("TestApp.runtimeconfig.json", """
            {
                "runtimeOptions": {
                    "framework": {
                        "name": "Microsoft.NETCore.App",
                        "version": "10.0.0"
                    }
                }
            }
            """);
        string binDir = Path.GetDirectoryName(configPath)!;
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths([binDir], frameworkRefs);

        Assert.True(result.ContainsKey("Microsoft.NETCore.App"));
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_RuntimeConfigWithFrameworksArray_Parsed()
    {
        string configPath = TestHelpers.WriteTempFile("TestApp.runtimeconfig.json", """
            {
                "runtimeOptions": {
                    "frameworks": [
                        { "name": "Microsoft.NETCore.App", "version": "10.0.0" },
                        { "name": "Microsoft.AspNetCore.App", "version": "10.0.0" }
                    ]
                }
            }
            """);
        string binDir = Path.GetDirectoryName(configPath)!;
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.NETCore.App",
            "Microsoft.AspNetCore.App"
        };

        var result = _sut.ResolveRuntimeAssemblyPaths([binDir], frameworkRefs);

        Assert.True(result.ContainsKey("Microsoft.NETCore.App"));
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_RuntimeConfigMissingRuntimeOptions_ReturnsEmpty()
    {
        string configPath = TestHelpers.WriteTempFile("TestApp.runtimeconfig.json", """{ "other": {} }""");
        string binDir = Path.GetDirectoryName(configPath)!;
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths([binDir], frameworkRefs);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveRuntimeAssemblyPaths_FrameworkElementMissingVersion_Skipped()
    {
        string configPath = TestHelpers.WriteTempFile("TestApp.runtimeconfig.json", """
            {
                "runtimeOptions": {
                    "framework": {
                        "name": "Microsoft.NETCore.App"
                    }
                }
            }
            """);
        string binDir = Path.GetDirectoryName(configPath)!;
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.NETCore.App" };

        var result = _sut.ResolveRuntimeAssemblyPaths([binDir], frameworkRefs);

        Assert.Empty(result);
    }

    private static List<string> FindBinDirectories()
    {
        string repoRoot = TestHelpers.FindRepoRoot();
        return Directory.GetDirectories(repoRoot, "bin", SearchOption.AllDirectories).ToList();
    }
}
