using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IProjectAssetsParser
{
    /// <summary>
    /// Parse a project.assets.json file and returns package to assembly mappings.
    /// </summary>
    /// <param name="projectAssetsPath">The file path to a project.assets.json file</param>
    /// <returns>Dictionary mapping assembly file names (lowercase) to package information</returns>
    Dictionary<string, Package>? ParseProjectAssets(string projectAssetsPath);
}
