namespace SourceExplorerMcp.Core.Models;

public sealed record AssemblyInfo(
    string PackageName,
    string AssemblyName,
    string Version,
    string FilePath,
    string? PublicKeyToken,
    string? TargetFramework,
    DateTime LastModified);
