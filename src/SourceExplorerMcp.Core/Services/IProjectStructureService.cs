using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public interface IProjectStructureService
{
    ProjectStructure Discover(string normalisedBasePath);
}
