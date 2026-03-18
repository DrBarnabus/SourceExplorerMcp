using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IProjectAssetsParser
{
    /// <summary>
    /// Parse a project.assets.json file and returns package to assembly mappings and framework references.
    /// </summary>
    /// <param name="projectAssetsPath">The file path to a project.assets.json file</param>
    /// <returns>Package mappings and framework references, or null if the file cannot be parsed</returns>
    ProjectAssetsData? ParseProjectAssets(string projectAssetsPath);
}
