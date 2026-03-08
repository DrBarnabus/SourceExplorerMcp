using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;

namespace SourceExplorerMcp.Core.Services;

public sealed class AssemblyMetadataExtractor(
    ILogger<AssemblyMetadataExtractor> logger)
    : IAssemblyMetadataExtractor
{
    private readonly ILogger<AssemblyMetadataExtractor> _logger = logger;

    public AssemblyMetadata? ExtractMetadata(string assemblyPath)
    {
        try
        {
            if (!File.Exists(assemblyPath))
            {
                _logger.LogWarning("Assembly file not found: {Path}", assemblyPath);
                return null;
            }

            using var fileStream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(fileStream);

            if (!peReader.HasMetadata)
            {
                _logger.LogWarning("Assembly file has no metadata: {Path}", assemblyPath);
                return null;
            }

            var metadataReader = peReader.GetMetadataReader();
            var assemblyDefinition = metadataReader.GetAssemblyDefinition();

            var assemblyName = assemblyDefinition.GetAssemblyName();
            string version = assemblyDefinition.Version.ToString();

            return new AssemblyMetadata(assemblyName.Name ?? assemblyName.FullName, version);
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogDebug(ex, "File is not a valid .NET assembly: {Path}", assemblyPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting metadata from assembly: {Path}", assemblyPath);
            return null;
        }
    }
}
