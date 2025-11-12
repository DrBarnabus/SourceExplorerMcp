namespace SourceExplorerMcp.Core.Models;

public sealed record DecompilationResult(
    string FullTypeName,
    string SourceCode,
    TypeInfo Type);
