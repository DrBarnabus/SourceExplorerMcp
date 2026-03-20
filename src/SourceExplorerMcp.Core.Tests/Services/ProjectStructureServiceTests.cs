using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class ProjectStructureServiceTests
{
    private readonly ProjectStructureService _sut = new(NullLogger<ProjectStructureService>.Instance);

    [Fact]
    public void Discover_EmptyDirectory_ReturnsZeroFingerprint()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = _sut.Discover(tempDir);

            Assert.Equal(0, result.Fingerprint);
            Assert.Empty(result.ProjectAssetsFiles);
            Assert.Empty(result.BinDirectories);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discover_WithSentinelFiles_ReturnsNonZeroFingerprint()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        string objDir = Path.Combine(tempDir, "obj");
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(objDir, "project.assets.json"), "{}");

        try
        {
            var result = _sut.Discover(tempDir);

            Assert.NotEqual(0, result.Fingerprint);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discover_ConsecutiveCallsWithoutChanges_ReturnsSameFingerprint()
    {
        string basePath = TestHelpers.FindRepoRoot();

        long first = _sut.Discover(basePath).Fingerprint;
        long second = _sut.Discover(basePath).Fingerprint;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Discover_ModifiedSentinelFile_ReturnsChangedFingerprint()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        string objDir = Path.Combine(tempDir, "obj");
        Directory.CreateDirectory(objDir);
        string assetsPath = Path.Combine(objDir, "project.assets.json");
        File.WriteAllText(assetsPath, "{\"version\": 1}");

        try
        {
            long before = _sut.Discover(tempDir).Fingerprint;

            File.WriteAllText(assetsPath, "{\"version\": 2}");

            long after = _sut.Discover(tempDir).Fingerprint;

            Assert.NotEqual(before, after);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discover_ReturnsProjectAssetsFiles()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        string objDir = Path.Combine(tempDir, "src", "MyProject", "obj");
        Directory.CreateDirectory(objDir);
        string assetsPath = Path.Combine(objDir, "project.assets.json");
        File.WriteAllText(assetsPath, "{}");

        try
        {
            var result = _sut.Discover(tempDir);

            Assert.Single(result.ProjectAssetsFiles);
            Assert.Equal(assetsPath, result.ProjectAssetsFiles[0]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Discover_ReturnsBinDirectories()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        string binDir = Path.Combine(tempDir, "src", "MyProject", "bin");
        Directory.CreateDirectory(binDir);

        try
        {
            var result = _sut.Discover(tempDir);

            Assert.Single(result.BinDirectories);
            Assert.Equal(binDir, result.BinDirectories[0]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
