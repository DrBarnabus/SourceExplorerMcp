using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;
using SourceExplorerMcp.Models;

namespace SourceExplorerMcp.Tools;

[McpServerToolType]
public sealed class SearchTypesTool(
    ILogger<SearchTypesTool> logger,
    ITypeSearchService typeSearchService)
{
    private readonly ILogger<SearchTypesTool> _logger = logger;
    private readonly ITypeSearchService _typeSearchService = typeSearchService;

    [McpServerTool(Name = "search-types"), Description("Search for .NET types across all assemblies in the project's dependency graph using wildcard patterns. Only works with .NET projects. Searches all types regardless of visibility. Does not search the project's own source code. Use the FullName from results as the FullName parameter of decompile-type to view source code.")]
    public async Task<SearchTypesOutput> SearchTypes(
        SearchTypesInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Searching types in assemblies from {Path}", basePath);

        var result = await _typeSearchService.SearchTypesAsync(basePath, input.SearchPattern, cancellationToken);
        return new SearchTypesOutput(result.Types.Select(TypeSummary.FromTypeInfo).ToList())
        {
            Diagnostics = result.Diagnostics is { Count: > 0 } ? result.Diagnostics : null
        };
    }
}

public sealed record SearchTypesInput
{
    [Description("Wildcard pattern to match type names (case-insensitive). * matches any characters, ? matches a single character. Matches against simple names (e.g. '*HttpClient*') and fully-qualified names (e.g. 'Microsoft.Extensions.*Options'). Prefer specific patterns to limit results.")]
    public required string SearchPattern { get; init; }

    [Description("Absolute path to the project or solution directory. Defaults to the current working directory if not provided.")]
    public string? ProjectBasePath { get; init; }
}

public sealed record SearchTypesOutput(List<TypeSummary> Types)
{
    [Description("Diagnostic messages explaining why results may be empty or incomplete, such as the project not being built.")]
    public List<string>? Diagnostics { get; init; }
}
