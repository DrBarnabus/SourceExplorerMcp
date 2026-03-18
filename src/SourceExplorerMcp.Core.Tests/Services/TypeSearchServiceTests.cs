using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Core.Tests.Services;

public sealed class TypeSearchServiceTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private readonly TypeSearchService _sut;

    public TypeSearchServiceTests()
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

        _sut = new TypeSearchService(
            NullLogger<TypeSearchService>.Instance,
            discoveryService,
            _cache);
    }

    [Fact]
    public async Task SearchTypesAsync_ExactSimpleName_FindsType()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "TypeSearchService", TestContext.Current.CancellationToken);

        Assert.Contains(results, t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
    }

    [Fact]
    public async Task SearchTypesAsync_WildcardPattern_ReturnsMultipleMatches()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "*SearchService", TestContext.Current.CancellationToken);

        Assert.True(results.Count >= 1);
        Assert.All(results, t => Assert.EndsWith("SearchService", GetSimpleName(t.FullName)));
    }

    [Fact]
    public async Task SearchTypesAsync_IsCaseInsensitive()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "typesearchservice", TestContext.Current.CancellationToken);

        Assert.Contains(results, t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
    }

    [Fact]
    public async Task SearchTypesAsync_MatchesAgainstFullyQualifiedName()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "SourceExplorerMcp.Core.Services.TypeSearchService", TestContext.Current.CancellationToken);

        Assert.NotEmpty(results);
        Assert.All(results, t => Assert.Equal("SourceExplorerMcp.Core.Services.TypeSearchService", t.FullName));
    }

    [Fact]
    public async Task SearchTypesAsync_NoMatches_ReturnsEmptyList()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "ZzzNonExistentTypeZzz", TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchTypesAsync_CompilerGeneratedTypesExcluded()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "*", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(results, t => GetSimpleName(t.FullName).StartsWith('<'));
        Assert.DoesNotContain(results, t => GetSimpleName(t.FullName).StartsWith("__"));
        Assert.DoesNotContain(results, t => t.FullName == "<Module>");
    }

    [Fact]
    public async Task SearchTypesAsync_ResultsIncludeDeclaration()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "TypeSearchService", TestContext.Current.CancellationToken);

        var match = results.First(t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
        Assert.Contains("sealed", match.Declaration);
        Assert.Contains("class", match.Declaration);
    }

    [Fact]
    public async Task SearchTypesAsync_GenericTypesIncludeTypeParameters()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "ILogger`1", TestContext.Current.CancellationToken);

        var match = results.First(t => t.FullName == "Microsoft.Extensions.Logging.ILogger`1");
        Assert.Contains("<", match.Declaration);
        Assert.Contains(">", match.Declaration);
    }

    [Fact]
    public async Task GetTypeInfoAsync_ExactMatch_ReturnsType()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "SourceExplorerMcp.Core.Services.TypeSearchService", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("SourceExplorerMcp.Core.Services.TypeSearchService", result.FullName);
    }

    [Fact]
    public async Task GetTypeInfoAsync_NonExistentType_ReturnsNull()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "NonExistent.Type.Name", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTypeInfoAsync_IsCaseInsensitive()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "sourceexplorermcp.core.services.typesearchservice", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchTypesAsync_FindsFrameworkTypes()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var results = await _sut.SearchTypesAsync(basePath, "HttpClient", TestContext.Current.CancellationToken);

        Assert.Contains(results, t => t.FullName == "System.Net.Http.HttpClient");
    }

    public void Dispose() => _cache.Dispose();

    private static string GetSimpleName(string fullName)
    {
        int lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
