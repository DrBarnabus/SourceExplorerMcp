using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class AssemblyMetadataExtractorTests
{
    private readonly AssemblyMetadataExtractor _sut = new(NullLogger<AssemblyMetadataExtractor>.Instance);

    [Fact]
    public void ExtractMetadata_NonExistentFile_ReturnsNull()
    {
        var result = _sut.ExtractMetadata("/non/existent/assembly.dll");

        Assert.Null(result);
    }

    [Fact]
    public void ExtractMetadata_ValidAssembly_ReturnsNameAndVersion()
    {
        string assemblyPath = FindCoreAssemblyPath();

        var result = _sut.ExtractMetadata(assemblyPath);

        Assert.NotNull(result);
        Assert.Equal("SourceExplorerMcp.Core", result.AssemblyName);
        Assert.NotNull(result.Version);
    }

    [Fact]
    public void ExtractMetadata_NonPeFile_ReturnsNull()
    {
        string path = TestHelpers.WriteTempFile("fake.dll", "this is not a PE file");

        var result = _sut.ExtractMetadata(path);

        Assert.Null(result);
    }

    private static string FindCoreAssemblyPath()
    {
        string repoRoot = TestHelpers.FindRepoRoot();
        string path = Path.Combine(repoRoot, "src", "SourceExplorerMcp.Core", "bin", "Debug", "net10.0", "SourceExplorerMcp.Core.dll");

        Assert.True(File.Exists(path), $"Assembly not found at {path}. Run 'dotnet build' first.");

        return path;
    }
}
