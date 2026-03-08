namespace SourceExplorerMcp.Core.Models;

public sealed record TypeInfo
{
    public required string FullName { get; init; }

    public required AssemblyInfo Assembly { get; init; }

    public required string Declaration { get; init; }
}
