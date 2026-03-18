using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class DecompilerServiceTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private readonly DecompilerService _sut;

    public DecompilerServiceTests()
    {
        var parser = new ProjectAssetsParser(NullLogger<ProjectAssetsParser>.Instance);
        var metadataExtractor = new AssemblyMetadataExtractor(NullLogger<AssemblyMetadataExtractor>.Instance);
        var runtimeResolver = new RuntimeAssemblyResolver(NullLogger<RuntimeAssemblyResolver>.Instance);

        var discoveryService = new AssemblyDiscoveryService(
            NullLogger<AssemblyDiscoveryService>.Instance,
            parser,
            metadataExtractor,
            runtimeResolver,
            _cache);

        var typeSearchService = new TypeSearchService(
            NullLogger<TypeSearchService>.Instance,
            discoveryService,
            _cache);

        _sut = new DecompilerService(
            NullLogger<DecompilerService>.Instance,
            typeSearchService);
    }

    [Fact]
    public async Task DecompileTypeAsync_KnownType_ReturnsSourceCode()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DecompileTypeAsync(
            basePath,
            "SourceExplorerMcp.Core.Models.AssemblyInfo",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("AssemblyInfo", result.SourceCode);
        Assert.Equal("SourceExplorerMcp.Core.Models.AssemblyInfo", result.FullTypeName);
    }

    [Fact]
    public async Task DecompileTypeAsync_NonExistentType_ReturnsNull()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DecompileTypeAsync(
            basePath,
            "NonExistent.Type.Name",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task DecompileTypeAsync_SignaturesMode_ExcludesMethodBodies()
    {
        string basePath = TestHelpers.FindRepoRoot();
        var options = new DecompilerOptions { DecompileMode = DecompileMode.Signatures };

        var result = await _sut.DecompileTypeAsync(
            basePath,
            "SourceExplorerMcp.Core.Services.ProjectAssetsParser",
            options,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("ParseProjectAssets", result.SourceCode);
    }

    [Fact]
    public async Task DecompileTypeAsync_FrameworkType_ReturnsSourceCode()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DecompileTypeAsync(
            basePath,
            "System.Net.Http.HttpClient",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("HttpClient", result.SourceCode);
    }

    public void Dispose() => _cache.Dispose();
}
