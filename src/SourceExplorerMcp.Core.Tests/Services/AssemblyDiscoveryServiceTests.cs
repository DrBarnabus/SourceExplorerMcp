using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class AssemblyDiscoveryServiceTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private readonly AssemblyDiscoveryService _sut;

    public AssemblyDiscoveryServiceTests()
    {
        var parser = new ProjectAssetsParser(NullLogger<ProjectAssetsParser>.Instance);
        var metadataExtractor = new AssemblyMetadataExtractor(NullLogger<AssemblyMetadataExtractor>.Instance);
        var runtimeResolver = new RuntimeAssemblyResolver(NullLogger<RuntimeAssemblyResolver>.Instance);

        _sut = new AssemblyDiscoveryService(
            NullLogger<AssemblyDiscoveryService>.Instance,
            parser,
            metadataExtractor,
            runtimeResolver,
            _cache);
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_IncludesNuGetPackageAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var assemblies = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(assemblies, a => a.PackageName == "ICSharpCode.Decompiler");
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_AttributesUnmappedDllsToFrameworkReference()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var assemblies = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(assemblies, a => a.PackageName == "Microsoft.NETCore.App");
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_IncludesFrameworkRuntimeAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var assemblies = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(assemblies, a => a.AssemblyName == "System.Net.Http");
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_NullBasePath_ThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _sut.DiscoverAssembliesAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_EmptyBasePath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.DiscoverAssembliesAsync("", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.DiscoverAssembliesAsync("/non/existent/directory", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_SecondCallReturnsCachedResult()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var first = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);
        var second = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_ResultsAreSortedByPackageNameThenAssemblyName()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var assemblies = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        var expected = assemblies
            .OrderBy(a => a.PackageName)
            .ThenBy(a => a.AssemblyName)
            .ToList();

        Assert.Equal(expected, assemblies);
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_ExcludesRefDirectoryAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var assemblies = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(assemblies, a =>
            a.FilePath.Contains(Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_CancellationThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        string basePath = TestHelpers.FindRepoRoot();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.DiscoverAssembliesAsync(basePath, cts.Token));
    }

    public void Dispose() => _cache.Dispose();
}
