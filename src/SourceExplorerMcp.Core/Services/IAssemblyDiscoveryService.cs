using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IAssemblyDiscoveryService
{
    /// <summary>
    /// Gets all discovered assemblies, using a cache if possible.
    /// </summary>
    Task<DiscoveryResult> DiscoverAssembliesAsync(string basePath, CancellationToken cancellationToken = default);
}
