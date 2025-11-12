namespace SourceExplorerMcp.Core.Models;

public sealed record TypeInfo
{
    public required string Name { get; set; }

    public required string FullName { get; set; }

    public string? Namespace { get; set; }

    public required AssemblyInfo Assembly { get; set; }

    public TypeKind Kind { get; set; }

    public TypeAccessibility Accessibility { get; set; }

    public bool IsAbstract { get; set; }

    public bool IsSealed { get; set; }

    public bool IsStatic { get; set; }

    public bool IsGeneric { get; set; }

    public int GenericParameterCount { get; set; }

    public List<string> GenericParameters { get; set; } = [];

    public string? BaseType { get; set; }

    public List<string> Interfaces { get; set; } = [];

    public bool IsNested { get; set; }

    public string? DeclaringType { get; set; }

    public int MetadataToken { get; set; }
}

public enum TypeKind
{
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Unknown
}

public enum TypeAccessibility
{
    Public,
    Internal,
    Private,
    Protected,
    ProtectedInternal,
    PrivateProtected,
    Unknown
}
