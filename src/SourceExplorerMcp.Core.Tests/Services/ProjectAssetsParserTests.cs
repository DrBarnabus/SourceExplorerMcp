using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class ProjectAssetsParserTests
{
    private readonly ProjectAssetsParser _sut = new(NullLogger<ProjectAssetsParser>.Instance);

    [Fact]
    public void ParseProjectAssets_WithRealProjectAssets_ReturnsPackageMappings()
    {
        string path = FindProjectAssetsPath();

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.NotEmpty(result.PackageMappings);
    }

    [Fact]
    public void ParseProjectAssets_WithRealProjectAssets_ReturnsFrameworkReferences()
    {
        string path = FindProjectAssetsPath();

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.NotEmpty(result.FrameworkReferences);
        Assert.Contains("Microsoft.NETCore.App", result.FrameworkReferences);
    }

    [Fact]
    public void ParseProjectAssets_WithNonExistentFile_ReturnsNull()
    {
        var result = _sut.ParseProjectAssets("/non/existent/project.assets.json");

        Assert.Null(result);
    }

    [Fact]
    public void ParseProjectAssets_PackageMappingsDoNotContainFrameworkAssemblies()
    {
        string path = FindProjectAssetsPath();

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.DoesNotContain("System.Net.Http.dll", result.PackageMappings.Keys);
    }

    [Fact]
    public void ParseProjectAssets_WithInvalidJson_ReturnsNull()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", "not valid json {{{");

        var result = _sut.ParseProjectAssets(path);

        Assert.Null(result);
    }

    [Fact]
    public void ParseProjectAssets_WithEmptyJsonObject_ReturnsEmptyData()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", "{}");

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Empty(result.PackageMappings);
        Assert.Empty(result.FrameworkReferences);
    }

    [Fact]
    public void ParseProjectAssets_ExcludesNonPackageLibraries()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {
                    "MyProject/1.0.0": {
                        "type": "project",
                        "files": ["lib/net10.0/MyProject.dll"]
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Empty(result.PackageMappings);
    }

    [Fact]
    public void ParseProjectAssets_SkipsInvalidLibraryKeys()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {
                    "InvalidKeyNoSlash": {
                        "type": "package",
                        "files": ["lib/net10.0/Something.dll"]
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Empty(result.PackageMappings);
    }

    [Fact]
    public void ParseProjectAssets_LibraryWithNoFilesProperty_ProducesEmptyAssemblySet()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {
                    "SomePackage/1.0.0": {
                        "type": "package"
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Empty(result.PackageMappings);
    }

    [Fact]
    public void ParseProjectAssets_DuplicateDllFileName_FirstPackageWins()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {
                    "PackageA/1.0.0": {
                        "type": "package",
                        "files": ["lib/net10.0/Shared.dll"]
                    },
                    "PackageB/2.0.0": {
                        "type": "package",
                        "files": ["lib/net10.0/Shared.dll"]
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.True(result.PackageMappings.ContainsKey("Shared.dll"));
        Assert.Equal("PackageA", result.PackageMappings["Shared.dll"].Name);
    }

    [Fact]
    public void ParseProjectAssets_FrameworkReferencesFromMultipleTargetFrameworks()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {},
                "project": {
                    "frameworks": {
                        "net10.0": {
                            "frameworkReferences": {
                                "Microsoft.NETCore.App": { "privateAssets": "all" }
                            }
                        },
                        "net9.0": {
                            "frameworkReferences": {
                                "Microsoft.AspNetCore.App": { "privateAssets": "all" }
                            }
                        }
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Contains("Microsoft.NETCore.App", result.FrameworkReferences);
        Assert.Contains("Microsoft.AspNetCore.App", result.FrameworkReferences);
    }

    [Fact]
    public void ParseProjectAssets_OnlyIncludesDllFiles()
    {
        string path = TestHelpers.WriteTempFile("project.assets.json", """
            {
                "libraries": {
                    "MyPackage/1.0.0": {
                        "type": "package",
                        "files": [
                            "lib/net10.0/MyPackage.dll",
                            "lib/net10.0/MyPackage.xml",
                            "lib/net10.0/MyPackage.pdb",
                            "MyPackage.nuspec"
                        ]
                    }
                }
            }
            """);

        var result = _sut.ParseProjectAssets(path);

        Assert.NotNull(result);
        Assert.Single(result.PackageMappings);
        Assert.True(result.PackageMappings.ContainsKey("MyPackage.dll"));
    }

    private static string FindProjectAssetsPath()
    {
        string repoRoot = TestHelpers.FindRepoRoot();
        string path = Path.Combine(repoRoot, "src", "SourceExplorerMcp", "obj", "project.assets.json");

        Assert.True(File.Exists(path), $"project.assets.json not found at {path}. Run 'dotnet restore' first.");
        return path;
    }
}
