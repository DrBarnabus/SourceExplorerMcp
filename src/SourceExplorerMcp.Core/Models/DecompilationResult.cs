namespace SourceExplorerMcp.Core.Models;

public sealed record DecompilationResult(
    string FullTypeName,
    string SourceCode,
    TypeInfo? Type)
{
    public List<string>? Diagnostics { get; init; }
}
