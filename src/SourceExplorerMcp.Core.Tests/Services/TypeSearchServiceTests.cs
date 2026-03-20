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
        var structureService = new ProjectStructureService(NullLogger<ProjectStructureService>.Instance);

        var discoveryService = new AssemblyDiscoveryService(
            NullLogger<AssemblyDiscoveryService>.Instance,
            parser,
            metadataExtractor,
            runtimeResolver,
            _cache,
            structureService);

        _sut = new TypeSearchService(
            NullLogger<TypeSearchService>.Instance,
            discoveryService,
            _cache);
    }

    [Fact]
    public async Task SearchTypesAsync_ExactSimpleName_FindsType()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "TypeSearchService", TestContext.Current.CancellationToken);

        Assert.Contains(result.Types, t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
    }

    [Fact]
    public async Task SearchTypesAsync_WildcardPattern_ReturnsMultipleMatches()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "*SearchService", TestContext.Current.CancellationToken);

        Assert.True(result.Types.Count >= 1);
        Assert.All(result.Types, t => Assert.EndsWith("SearchService", GetSimpleName(t.FullName)));
    }

    [Fact]
    public async Task SearchTypesAsync_IsCaseInsensitive()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "typesearchservice", TestContext.Current.CancellationToken);

        Assert.Contains(result.Types, t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
    }

    [Fact]
    public async Task SearchTypesAsync_MatchesAgainstFullyQualifiedName()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "SourceExplorerMcp.Core.Services.TypeSearchService", TestContext.Current.CancellationToken);

        Assert.NotEmpty(result.Types);
        Assert.All(result.Types, t => Assert.Equal("SourceExplorerMcp.Core.Services.TypeSearchService", t.FullName));
    }

    [Fact]
    public async Task SearchTypesAsync_NoMatches_ReturnsEmptyList()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "ZzzNonExistentTypeZzz", TestContext.Current.CancellationToken);

        Assert.Empty(result.Types);
    }

    [Fact]
    public async Task SearchTypesAsync_CompilerGeneratedTypesExcluded()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "*", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result.Types, t => GetSimpleName(t.FullName).StartsWith('<'));
        Assert.DoesNotContain(result.Types, t => GetSimpleName(t.FullName).StartsWith("__"));
        Assert.DoesNotContain(result.Types, t => t.FullName == "<Module>");
    }

    [Fact]
    public async Task SearchTypesAsync_ResultsIncludeDeclaration()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "TypeSearchService", TestContext.Current.CancellationToken);

        var match = result.Types.First(t => t.FullName == "SourceExplorerMcp.Core.Services.TypeSearchService");
        Assert.Contains("sealed", match.Declaration);
        Assert.Contains("class", match.Declaration);
    }

    [Fact]
    public async Task SearchTypesAsync_GenericTypesIncludeTypeParameters()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "ILogger`1", TestContext.Current.CancellationToken);

        var match = result.Types.First(t => t.FullName == "Microsoft.Extensions.Logging.ILogger`1");
        Assert.Contains("<", match.Declaration);
        Assert.Contains(">", match.Declaration);
    }

    [Fact]
    public async Task GetTypeInfoAsync_ExactMatch_ReturnsType()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "SourceExplorerMcp.Core.Services.TypeSearchService", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Type);
        Assert.Equal("SourceExplorerMcp.Core.Services.TypeSearchService", result.Type.FullName);
    }

    [Fact]
    public async Task GetTypeInfoAsync_NonExistentType_ReturnsNullType()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "NonExistent.Type.Name", TestContext.Current.CancellationToken);

        Assert.Null(result.Type);
    }

    [Fact]
    public async Task GetTypeInfoAsync_IsCaseInsensitive()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.GetTypeInfoAsync(basePath, "sourceexplorermcp.core.services.typesearchservice", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Type);
    }

    [Fact]
    public async Task SearchTypesAsync_FindsFrameworkTypes()
    {
        string basePath = TestHelpers.FindRepoRoot();

        var result = await _sut.SearchTypesAsync(basePath, "HttpClient", TestContext.Current.CancellationToken);

        Assert.Contains(result.Types, t => t.FullName == "System.Net.Http.HttpClient");
    }

    public void Dispose() => _cache.Dispose();

    private static string GetSimpleName(string fullName)
    {
        int lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
