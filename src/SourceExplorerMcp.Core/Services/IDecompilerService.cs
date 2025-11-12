using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IDecompilerService
{
    Task<DecompilationResult?> DecompileTypeAsync(
        string basePath,
        string fullTypeName,
        DecompilerOptions? options = null,
        CancellationToken cancellationToken = default);
}
