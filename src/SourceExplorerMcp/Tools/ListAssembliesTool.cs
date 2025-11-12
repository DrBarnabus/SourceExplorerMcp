using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Tools;

[McpServerToolType]
public sealed class ListAssembliesTool(
    ILogger<ListAssembliesTool> logger,
    IAssemblyDiscoveryService assemblyDiscoveryService)
{
    private readonly ILogger<ListAssembliesTool> _logger = logger;
    private readonly IAssemblyDiscoveryService _assemblyDiscoveryService = assemblyDiscoveryService;

    [McpServerTool(Name = "list-all-assemblies"), Description("Discover what NuGet packages and framework assemblies are available in your project. Use this tool to: check if a specific package is installed before searching for types (e.g., 'Is Serilog available?'), see package versions to understand compatibility or find outdated dependencies, explore what's available when working in an unfamiliar codebase, verify that NuGet restore completed successfully, or get an overview of the project's dependency landscape. Start here when exploring a new codebase or before using search-types to narrow down which assemblies to focus on.")]
    public async Task<ListAllAssembliesOutput> ListAssemblies(
        ListAllAssembliesInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Listing assemblies from {Path}", basePath);

        var assemblies = await _assemblyDiscoveryService.DiscoverAssembliesAsync(basePath, cancellationToken);
        return new ListAllAssembliesOutput(assemblies);
    }
}

public sealed record ListAllAssembliesInput
{
    [Description("Optional project path override")]
    public string? ProjectBasePath { get; init; }
}

public sealed record ListAllAssembliesOutput(List<AssemblyInfo> Assemblies);
