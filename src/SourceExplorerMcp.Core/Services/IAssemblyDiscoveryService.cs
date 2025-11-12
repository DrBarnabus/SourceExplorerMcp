using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IAssemblyDiscoveryService
{
    /// <summary>
    /// Gets all discovered assemblies, using a cache if possible.
    /// </summary>
    Task<List<AssemblyInfo>> DiscoverAssembliesAsync(string basePath, CancellationToken cancellationToken = default);
}
