namespace SourceExplorerMcp.Core.Models;

public sealed record ProjectAssetsData(
    Dictionary<string, Package> PackageMappings,
    HashSet<string> FrameworkReferences);
