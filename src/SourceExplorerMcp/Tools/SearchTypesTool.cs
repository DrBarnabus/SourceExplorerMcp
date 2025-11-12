using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Tools;

[McpServerToolType]
public sealed class SearchTypesTool(
    ILogger<SearchTypesTool> logger,
    ITypeSearchService typeSearchService)
{
    private readonly ILogger<SearchTypesTool> _logger = logger;
    private readonly ITypeSearchService _typeSearchService = typeSearchService;

    [McpServerTool(Name = "search-types"), Description("Quickly find .NET types across all packages in your project using wildcard patterns. Use when you: don't know the exact namespace of a type (e.g., '*HttpClient*', '*JsonConverter*'), want to discover what types are available in a package, need to find all implementations or related types (e.g., '*Exception', '*Attribute'), or are exploring unfamiliar NuGet packages. Supports * for any characters and ? for single character matching against both simple names and full names (including namespaces).")]
    public async Task<SearchTypesOutput> SearchTypes(
        SearchTypesInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Searching types in assemblies from {Path}", basePath);

        var matchingTypes = await _typeSearchService.SearchTypesAsync(basePath, input.SearchPattern, cancellationToken);
        return new SearchTypesOutput(matchingTypes);
    }
}

public sealed record SearchTypesInput
{
    [Description("Wildcard pattern to match type names (case-insensitive). Use * for any characters, ? for single character. Matches against both simple names and full names (including namespaces).")]
    public required string SearchPattern { get; init; }

    [Description("Optional project path override")]
    public string? ProjectBasePath { get; init; }
}

public sealed record SearchTypesOutput(List<TypeInfo> Types);
