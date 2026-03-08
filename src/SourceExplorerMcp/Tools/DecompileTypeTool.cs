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

    [McpServerTool(Name = "decompile-type"), Description("Decompile a .NET type from the project's dependency graph into C# source code. Only works with .NET projects. The type must exist in a NuGet package or framework assembly resolved by the project. Returns null when the type is not found. Use search-types first if you do not know the exact full type name.")]
    public async Task<DecompileTypeOutput> DecompileType(
        DecompileTypeInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Decompiling type '{FullName}' in assemblies from {Path}", input.FullName, basePath);

        var options = new DecompilerOptions
        {
            IncludeXmlDocumentation = input.IncludeXmlDocumentation ?? true,
            ShowCompilerGeneratedCode = input.ShowCompilerGeneratedCode ?? false,
            LanguageVersion = LanguageVersion.Latest
        };

        var result = await _decompilerService.DecompileTypeAsync(basePath, input.FullName, options, cancellationToken);
        return new DecompileTypeOutput(result);
    }
}

public sealed record DecompileTypeInput
{
    [Description("Fully-qualified type name including namespace (e.g. 'System.String', 'Microsoft.Extensions.Logging.ILogger'). For generic types, use backtick notation with arity (e.g. 'System.Collections.Generic.List`1'). For nested types, use + as separator (e.g. 'System.Environment+SpecialFolder').")]
    public required string FullName { get; init; }

    [Description("Include XML documentation comments in the decompiled output. Set to false to reduce output size when you only need the implementation. Defaults to true.")]
    public bool? IncludeXmlDocumentation { get; init; }

    [Description("Include compiler-generated members such as backing fields, async state machines, and iterator implementations. Useful for understanding auto-properties, async methods, or yield-based iterators. Defaults to false.")]
    public bool? ShowCompilerGeneratedCode { get; init; }

    [Description("Absolute path to the project or solution directory. Defaults to the current working directory if not provided.")]
    public string? ProjectBasePath { get; init; }
}

public sealed record DecompileTypeOutput(DecompilationResult? Result);
