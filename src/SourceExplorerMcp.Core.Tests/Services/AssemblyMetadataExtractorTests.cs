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
        string assemblyPath = typeof(AssemblyMetadataExtractor).Assembly.Location;

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
}
