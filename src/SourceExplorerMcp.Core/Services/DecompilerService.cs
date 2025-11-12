using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;
using TypeKind = ICSharpCode.Decompiler.TypeSystem.TypeKind;

namespace SourceExplorerMcp.Core.Services;

public sealed class DecompilerService(
    ILogger<DecompilerService> logger,
    ITypeSearchService typeSearchService)
    : IDecompilerService
{
    private readonly ILogger<DecompilerService> _logger = logger;
    private readonly ITypeSearchService _typeSearchService = typeSearchService;

    public async Task<DecompilationResult?> DecompileTypeAsync(string basePath, string fullTypeName, DecompilerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        options ??= new DecompilerOptions();

        _logger.LogInformation("Decompiling type '{TypeName}' from path: {Path}", fullTypeName, basePath);

        var typeInfo = await _typeSearchService.GetTypeInfoAsync(basePath, fullTypeName, cancellationToken);
        if (typeInfo is null)
        {
            _logger.LogWarning("Type not found: {TypeName}", fullTypeName);
            return null;
        }

        try
        {
            var sourceCode = DecompileTypeInternal(typeInfo, options);

            _logger.LogInformation("Successfully decompiled type: {TypeName}, returning {CharCount} chars of source code", fullTypeName, sourceCode.Length);

            return new DecompilationResult(
                fullTypeName,
                sourceCode,
                typeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decompiling type: {TypeName}", fullTypeName);
            throw;
        }
    }

    private string DecompileTypeInternal(TypeInfo typeInfo, DecompilerOptions options)
    {
        string assemblyPath = typeInfo.Assembly.FilePath;

        var decompilerSettings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            ShowXmlDocumentation = options.IncludeXmlDocumentation,
            RemoveDeadCode = true,
            RemoveDeadStores = true,
            UseSdkStyleProjectFormat = true,
            UseEnhancedUsing = true,
            UseExpressionBodyForCalculatedGetterOnlyProperties = true
        };

        if (options.LanguageVersion != LanguageVersion.Latest)
            decompilerSettings.SetLanguageVersion(options.LanguageVersion);

        var decompiler = new CSharpDecompiler(assemblyPath, decompilerSettings);

        try
        {
            var fullTypeName = new FullTypeName(typeInfo.FullName);
            return decompiler.DecompileTypeAsString(fullTypeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decompile type by name, trying fallback approach: {TypeName}", typeInfo.Name);

            try
            {
                var type = decompiler.TypeSystem.MainModule.Compilation.FindType(new FullTypeName(typeInfo.FullName));
                if (type != null && type.Kind != TypeKind.Unknown)
                {
                    var typeDefinition = type.GetDefinition();
                    if (typeDefinition != null)
                        return decompiler.DecompileTypeAsString(new FullTypeName(typeDefinition.FullName));
                }

                throw new InvalidOperationException($"Could not find type '{typeInfo.FullName}' in assembly");
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException($"Failed to decompile type '{typeInfo.FullName}'. The type may be obfuscated, compiler-generated, or in an unsupported format.", innerException);
            }
        }
    }
}
