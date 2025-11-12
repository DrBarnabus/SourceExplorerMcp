namespace SourceExplorerMcp.Core.Models;

public sealed record Package(
    string Name,
    string Version,
    HashSet<string> AssemblyFiles);
