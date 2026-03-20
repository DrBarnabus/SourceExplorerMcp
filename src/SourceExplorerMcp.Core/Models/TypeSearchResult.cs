namespace SourceExplorerMcp.Core.Models;

public sealed record TypeSearchResult(
    List<TypeInfo> Types,
    List<string> Diagnostics);
