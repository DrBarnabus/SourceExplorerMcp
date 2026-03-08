using ICSharpCode.Decompiler.CSharp;

namespace SourceExplorerMcp.Core.Models;

public sealed record DecompilerOptions
{
    public bool IncludeXmlDocumentation { get; set; } = true;

    public bool ShowCompilerGeneratedCode { get; set; } = false;

    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Latest;

    public DecompileMode DecompileMode { get; set; } = DecompileMode.Full;
}
