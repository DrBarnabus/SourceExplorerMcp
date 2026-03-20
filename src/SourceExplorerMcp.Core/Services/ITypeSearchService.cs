using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface ITypeSearchService
{
    Task<TypeSearchResult> SearchTypesAsync(string basePath, string searchPattern, CancellationToken cancellationToken = default);

    Task<TypeLookupResult> GetTypeInfoAsync(string basePath, string fullTypeName, CancellationToken cancellationToken = default);
}
