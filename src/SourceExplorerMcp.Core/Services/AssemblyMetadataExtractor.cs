using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
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

            string? publicKeyToken = null;
            if (!assemblyDefinition.PublicKey.IsNil)
            {
                byte[] publicKeyBytes = metadataReader.GetBlobBytes(assemblyDefinition.PublicKey);
                if (publicKeyBytes.Length > 0)
                    publicKeyToken = ComputePublicKeyToken(publicKeyBytes);
            }

            string? culture = null;
            if (!assemblyDefinition.Culture.IsNil)
            {
                culture = metadataReader.GetString(assemblyDefinition.Culture);
                if (string.IsNullOrWhiteSpace(culture))
                    culture = "neutral";
            }

            return new AssemblyMetadata(assemblyName.Name ?? assemblyName.FullName, version, publicKeyToken, culture);
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

        static string? ComputePublicKeyToken(byte[] bytes)
        {
            try
            {
                Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
                SHA1.HashData(bytes, hash);

                Span<byte> token = stackalloc byte[8];
                for (int i = 0; i < 8; i++)
                    token[i] = hash[hash.Length - 1 - i];

                return Convert.ToHexStringLower(token);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
