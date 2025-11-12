using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IAssemblyMetadataExtractor
{
    /// <summary>
    /// Extract metadata from an assembly file.
    /// </summary>
    /// <param name="assemblyPath">Full path to the assembly file</param>
    /// <returns>Assembly metadata or null if extraction fails</returns>
    AssemblyMetadata? ExtractMetadata(string assemblyPath);
}
