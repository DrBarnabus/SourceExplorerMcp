using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface ITypeSearchService
{
    Task<List<TypeInfo>> SearchTypesAsync(string basePath, string searchPattern, CancellationToken cancellationToken = default);

    Task<TypeInfo?> GetTypeInfoAsync(string basePath, string fullTypeName, CancellationToken cancellationToken = default);
}
