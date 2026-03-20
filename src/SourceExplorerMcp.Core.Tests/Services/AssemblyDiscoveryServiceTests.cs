using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
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

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(result.Assemblies, a => a.PackageName == "ICSharpCode.Decompiler");
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_AttributesUnmappedDllsToFrameworkReference()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(result.Assemblies, a => a.PackageName == "Microsoft.NETCore.App");
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_IncludesFrameworkRuntimeAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.Contains(result.Assemblies, a => a.AssemblyName == "System.Net.Http");
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

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        var expected = result.Assemblies
            .OrderBy(a => a.PackageName)
            .ThenBy(a => a.AssemblyName)
            .ToList();

        Assert.Equal(expected, result.Assemblies);
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_ExcludesRefDirectoryAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result.Assemblies, a =>
            a.FilePath.Contains(Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_NoDuplicateAssemblies()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.DiscoverAssembliesAsync(basePath, TestContext.Current.CancellationToken);

        var duplicates = result.Assemblies
            .GroupBy(a => (a.PackageName, a.AssemblyName))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.PackageName}/{g.Key.AssemblyName} ({g.Count()}x)")
            .ToList();

        Assert.Empty(duplicates);
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

    [Fact]
    public async Task DiscoverAssembliesAsync_EmptyDirectory_ReturnsDiagnostics()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = await _sut.DiscoverAssembliesAsync(tempDir, TestContext.Current.CancellationToken);

            Assert.Empty(result.Assemblies);
            Assert.NotEmpty(result.Diagnostics);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAssembliesAsync_EmptyDirectory_IsNotCached()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"source-explorer-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var first = await _sut.DiscoverAssembliesAsync(tempDir, TestContext.Current.CancellationToken);
            var second = await _sut.DiscoverAssembliesAsync(tempDir, TestContext.Current.CancellationToken);

            Assert.NotSame(first, second);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    public void Dispose() => _cache.Dispose();
}
