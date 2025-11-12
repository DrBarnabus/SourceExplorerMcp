using System.ComponentModel;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SourceExplorerMcp.Core.Models;
using SourceExplorerMcp.Core.Services;

namespace SourceExplorerMcp.Tools;

[McpServerToolType]
public sealed class DecompileTypeTool(
    ILogger<DecompileTypeTool> logger,
    IDecompilerService decompilerService)
{
    private readonly ILogger<DecompileTypeTool> _logger = logger;
    private readonly IDecompilerService _decompilerService = decompilerService;

    [McpServerTool(Name = "decompile-type"), Description("Instantly view the source code of any .NET type - including third-party NuGet packages, framework types, and dependencies. Perfect when you need to: understand method signatures and parameter types you're unfamiliar with, see how a library actually implements something under the hood, debug issues with types you don't have source access to, discover available methods/properties on framework types, or understand extension methods and their implementations. Much faster than searching GitHub or reading documentation. Returns complete C# source code including all methods, properties, fields and nested types. Use this tool whenever you encounter an unfamiliar type or need to understand implementation details.")]
    public async Task<DecompileTypeOutput> DecompileType(
        DecompileTypeInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Decompiling type '{FullTypeName}' in assemblies from {Path}", input.FullTypeName, basePath);

        var options = new DecompilerOptions
        {
            IncludeXmlDocumentation = input.IncludeXmlDocumentation ?? true,
            ShowCompilerGeneratedCode = input.ShowCompilerGeneratedCode ?? false,
            LanguageVersion = LanguageVersion.Latest
        };

        var result = await _decompilerService.DecompileTypeAsync(basePath, input.FullTypeName, options, cancellationToken);
        return new DecompileTypeOutput(result);
    }
}

public sealed record DecompileTypeInput
{
    [Description("Full name of the type to decompile, including namespace (e.g. 'System.String', 'System.Collections.Generic.List`1')")]
    public required string FullTypeName { get; init; }

    [Description("Include XML documentation comments in the output (default: true)")]
    public bool? IncludeXmlDocumentation { get; init; }

    [Description("Show compiler-generated code like backing fields (default: false)")]
    public bool? ShowCompilerGeneratedCode { get; init; }

    [Description("Optional project path override")]
    public string? ProjectBasePath { get; init; }
}

public sealed record DecompileTypeOutput(DecompilationResult? Result);
