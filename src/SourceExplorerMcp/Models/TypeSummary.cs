using System.ComponentModel;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Models;

public sealed record TypeSummary
{
    [Description("Fully-qualified type name. Use as the FullName parameter of decompile-type.")]
    public required string FullName { get; init; }

    [Description("C# type declaration (e.g. 'public sealed class HttpClient').")]
    public required string Declaration { get; init; }

    [Description("NuGet package name and version (e.g. 'Microsoft.NETCore.App.Ref v10.0.0').")]
    public required string Package { get; init; }

    public static TypeSummary FromTypeInfo(TypeInfo typeInfo) => new()
    {
        FullName = typeInfo.FullName,
        Declaration = typeInfo.Declaration,
        Package = $"{typeInfo.Assembly.PackageName} v{typeInfo.Assembly.Version}"
    };
}
