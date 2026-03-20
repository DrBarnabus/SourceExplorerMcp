namespace SourceExplorerMcp.Core.Models;

public sealed record DiscoveryResult(
    List<AssemblyInfo> Assemblies,
    List<string> Diagnostics,
    long Fingerprint);
