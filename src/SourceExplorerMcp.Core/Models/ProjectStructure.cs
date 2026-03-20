namespace SourceExplorerMcp.Core.Models;

public sealed record ProjectStructure(
    IReadOnlyList<string> ProjectAssetsFiles,
    IReadOnlyList<string> BinDirectories,
    long Fingerprint);
