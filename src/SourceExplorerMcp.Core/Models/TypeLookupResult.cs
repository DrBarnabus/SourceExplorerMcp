namespace SourceExplorerMcp.Core.Models;

public sealed record TypeLookupResult(
    TypeInfo? Type,
    List<string> Diagnostics);
