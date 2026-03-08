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
    private const int DefaultMaxInlineChars = 10000;

    private readonly ILogger<DecompileTypeTool> _logger = logger;
    private readonly IDecompilerService _decompilerService = decompilerService;

    [McpServerTool(Name = "decompile-type"), Description("Decompile a .NET type from the project's dependency graph into C# source code. Only works with .NET projects. The type must exist in a NuGet package or framework assembly resolved by the project. Returns null when the type is not found. Use search-types first if you do not know the exact full type name. Large outputs are saved to a file and the path is returned.")]
    public async Task<DecompileTypeOutput> DecompileType(
        DecompileTypeInput input,
        CancellationToken cancellationToken = default)
    {
        string basePath = input.ProjectBasePath ?? Environment.CurrentDirectory;
        _logger.LogInformation("Decompiling type '{FullName}' in assemblies from {Path}", input.FullName, basePath);

        var decompileMode = input.DecompileMode?.ToLowerInvariant() switch
        {
            "signatures" => DecompileMode.Signatures,
            _ => DecompileMode.Full
        };

        var options = new DecompilerOptions
        {
            IncludeXmlDocumentation = input.IncludeXmlDocumentation ?? true,
            ShowCompilerGeneratedCode = input.ShowCompilerGeneratedCode ?? false,
            LanguageVersion = LanguageVersion.Latest,
            DecompileMode = decompileMode
        };

        var result = await _decompilerService.DecompileTypeAsync(basePath, input.FullName, options, cancellationToken);
        if (result is null)
            return new DecompileTypeOutput();

        var threshold = GetMaxInlineChars();
        var sourceCode = result.SourceCode;
        var charCount = sourceCode.Length;
        var lineCount = sourceCode.AsSpan().Count('\n') + 1;

        if (charCount <= threshold)
        {
            return new DecompileTypeOutput
            {
                Type = result.Type,
                Source = new DecompiledSource
                {
                    Content = sourceCode,
                    LineCount = lineCount,
                    CharCount = charCount
                }
            };
        }

        var filePath = WriteToTempFile(result.FullTypeName, sourceCode);
        return new DecompileTypeOutput
        {
            Type = result.Type,
            Source = new DecompiledSource
            {
                FilePath = filePath,
                LineCount = lineCount,
                CharCount = charCount,
                Message = $"The decompiled source for '{result.FullTypeName}' is {charCount} characters ({lineCount} lines), which exceeds the inline threshold of {threshold} characters. The source has been written to the 'filePath' in this response. Read the file to view the source code, or use decompileMode 'signatures' for a compact API overview."
            }
        };
    }

    private static int GetMaxInlineChars()
    {
        var envValue = Environment.GetEnvironmentVariable("SOURCE_EXPLORER_MAX_INLINE_CHARS");
        if (envValue is not null && int.TryParse(envValue, out var parsed) && parsed > 0)
            return parsed;

        return DefaultMaxInlineChars;
    }

    private static string WriteToTempFile(string fullTypeName, string sourceCode)
    {
        var directory = Path.Combine(Path.GetTempPath(), "source-explorer-mcp");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, GetFileName(fullTypeName));
        File.WriteAllText(filePath, sourceCode);

        return filePath;
    }

    private static string GetFileName(string typeName)
    {
        const int suffixLength = 8;

        // {sanitised-type-name}-{random-suffix}.cs
        return string.Create(typeName.Length + 1 + suffixLength + 3, typeName, static (span, typeName) =>
        {
            typeName.AsSpan().CopyTo(span);

            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < typeName.Length; i++)
            {
                if (Array.IndexOf(invalidChars, span[i]) >= 0)
                    span[i] = '_';
            }

            span[typeName.Length] = '-';

            Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", span.Slice(typeName.Length + 1, suffixLength));
            ".cs".CopyTo(span[(typeName.Length + 1 + suffixLength)..]);
        });
    }
}

public sealed record DecompileTypeInput
{
    [Description("Fully-qualified type name including namespace (e.g. 'System.String', 'Microsoft.Extensions.Logging.ILogger'). For generic types, use backtick notation with arity (e.g. 'System.Collections.Generic.List`1'). For nested types, use + as separator (e.g. 'System.Environment+SpecialFolder').")]
    public required string FullName { get; init; }

    [Description("Controls the level of detail in the decompiled output. 'full' (default) returns complete source code including method bodies. 'signatures' returns only the type's API surface with member signatures and no method bodies.")]
    public string? DecompileMode { get; init; }

    [Description("Include XML documentation comments in the decompiled output. Set to false to reduce output size when you only need the implementation. Defaults to true.")]
    public bool? IncludeXmlDocumentation { get; init; }

    [Description("Include compiler-generated members such as backing fields, async state machines, and iterator implementations. Useful for understanding auto-properties, async methods, or yield-based iterators. Defaults to false.")]
    public bool? ShowCompilerGeneratedCode { get; init; }

    [Description("Absolute path to the project or solution directory. Defaults to the current working directory if not provided.")]
    public string? ProjectBasePath { get; init; }
}

public sealed record DecompileTypeOutput
{
    [Description("Type metadata, or null if not found.")]
    public TypeInfo? Type { get; init; }

    [Description("Decompiled source, or null if not found.")]
    public DecompiledSource? Source { get; init; }
}

public sealed record DecompiledSource
{
    [Description("Decompiled source code, or null if offloaded to a file.")]
    public string? Content { get; init; }

    [Description("Path to temporary file with the source, or null if inline.")]
    public string? FilePath { get; init; }

    public int LineCount { get; init; }

    public int CharCount { get; init; }

    [Description("Guidance when source was offloaded to a file.")]
    public string? Message { get; init; }
}
