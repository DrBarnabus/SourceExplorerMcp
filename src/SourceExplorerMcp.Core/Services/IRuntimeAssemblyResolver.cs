namespace SourceExplorerMcp.Core.Services;

public interface IRuntimeAssemblyResolver
{
    /// <summary>
    /// Discovers framework assembly paths from the shared .NET runtime based on runtimeconfig.json files
    /// found in the given bin directories.
    /// </summary>
    /// <param name="binDirectories">Bin directories to scan for runtimeconfig.json files</param>
    /// <param name="frameworkReferences">Framework reference names from project.assets.json to resolve</param>
    /// <returns>
    /// Dictionary mapping framework reference names to lists of assembly file paths.
    /// Only includes frameworks that are also present in the provided framework references set.
    /// </returns>
    Dictionary<string, List<string>> ResolveRuntimeAssemblyPaths(IReadOnlyList<string> binDirectories, HashSet<string> frameworkReferences);
}
