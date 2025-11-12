using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SourceExplorerMcp.Core.Models;
using TypeInfo = SourceExplorerMcp.Core.Models.TypeInfo;

namespace SourceExplorerMcp.Core.Services;

public sealed class TypeSearchService(
    ILogger<TypeSearchService> logger,
    IAssemblyDiscoveryService assemblyDiscoveryService,
    IMemoryCache cache)
    : ITypeSearchService
{
    private const string CacheKeyPrefix = "types:";

    private readonly ILogger<TypeSearchService> _logger = logger;
    private readonly IAssemblyDiscoveryService _assemblyDiscoveryService = assemblyDiscoveryService;
    private readonly IMemoryCache _cache = cache;

    public async Task<List<TypeInfo>> SearchTypesAsync(string basePath, string searchPattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        string normalisedPath = Path.GetFullPath(basePath);
        _logger.LogInformation("Searching for types matching '{Pattern}' in: {Path}", searchPattern, basePath);

        var allTypes = await GetAllTypesAsync(normalisedPath, cancellationToken);

        var regex = WildcardToRegex(searchPattern);

        var matchingTypes = allTypes
            .Where(t => regex.IsMatch(t.Name) || regex.IsMatch(t.FullName))
            .OrderBy(t => t.Namespace ?? string.Empty)
            .ThenBy(t => t.Name)
            .ToList();

        _logger.LogInformation("Found {Count} types matching pattern '{Pattern}'", matchingTypes.Count, searchPattern);

        return matchingTypes;
    }

    public async Task<TypeInfo?> GetTypeInfoAsync(string basePath, string fullTypeName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        string normalisedPath = Path.GetFullPath(basePath);
        _logger.LogInformation("Searching for type matching exactly '{FullTypeName}' in: {Path}", fullTypeName, basePath);

        var allTypes = await GetAllTypesAsync(normalisedPath, cancellationToken);
        return allTypes.FirstOrDefault(t => t.FullName.Equals(fullTypeName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<TypeInfo>> GetAllTypesAsync(string normalisedPath, CancellationToken cancellationToken)
    {
        string cacheKey = GetCacheKey(normalisedPath);

        if (_cache.TryGetValue<List<TypeInfo>>(cacheKey, out var cachedTypes) && cachedTypes is not null)
        {
            _logger.LogDebug("Retrieved {Count} types from cache for path: {Path}", cachedTypes.Count, normalisedPath);
            return cachedTypes;
        }

        _logger.LogInformation("Building type index for path: {Path}", normalisedPath);

        var assemblies = await _assemblyDiscoveryService.DiscoverAssembliesAsync(normalisedPath, cancellationToken);

        var allTypes = await Task.Run(() => ExtractTypesFromAssemblies(assemblies, cancellationToken), cancellationToken);

        _cache.Set(cacheKey, assemblies);

        _logger.LogInformation("Indexed {Count} types from {AssemblyCount} assemblies", allTypes.Count, assemblies.Count);
        return allTypes;
    }

    private List<TypeInfo> ExtractTypesFromAssemblies(List<AssemblyInfo> assemblies, CancellationToken cancellationToken)
    {
        var allTypes = new List<TypeInfo>();

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var types = ExtractTypesFromAssembly(assembly);
                allTypes.AddRange(types);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting types from assembly: {Path}", assembly.FilePath);
            }
        }

        return allTypes;
    }

    private List<TypeInfo> ExtractTypesFromAssembly(AssemblyInfo assembly)
    {
        var types = new List<TypeInfo>();

        try
        {
            using var fileStream = File.OpenRead(assembly.FilePath);
            using var peReader = new PEReader(fileStream);

            if (!peReader.HasMetadata)
                return types;

            var metadataReader = peReader.GetMetadataReader();
            foreach (var typeDefinitionHandle in metadataReader.TypeDefinitions)
            {
                try
                {
                    var typeDefinition = metadataReader.GetTypeDefinition(typeDefinitionHandle);

                    string name = metadataReader.GetString(typeDefinition.Name);
                    if (IsCompilerGenerated(name))
                        continue;

                    var typeInfo = CreateTypeInfo(typeDefinition, typeDefinitionHandle, metadataReader, assembly);
                    if (typeInfo is not null)
                        types.Add(typeInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error processing type definition in {Assembly}", assembly.AssemblyName);
                }
            }
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogDebug(ex, "File is not a valid .NET assembly: {Path}", assembly.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading assembly: {Path}", assembly.FilePath);
        }

        return types;
    }

    private TypeInfo? CreateTypeInfo(
        TypeDefinition typeDefinition,
        TypeDefinitionHandle typeDefinitionHandle,
        MetadataReader metadataReader,
        AssemblyInfo assembly)
    {
        try
        {
            string name = metadataReader.GetString(typeDefinition.Name);
            string? namespaceName = typeDefinition.Namespace.IsNil ? null : metadataReader.GetString(typeDefinition.Namespace);
            string fullName = string.IsNullOrEmpty(namespaceName) ? name : $"{namespaceName}.{name}";
            var attributes = typeDefinition.Attributes;
            var typeKind = DetermineTypeKind(typeDefinition, metadataReader);
            var accessibility = DetermineAccessibility(attributes);

            var genericParams = typeDefinition.GetGenericParameters()
                .Select(metadataReader.GetGenericParameter)
                .Select(gp => metadataReader.GetString(gp.Name))
                .ToList();

            string? baseType = null;
            if (!typeDefinition.BaseType.IsNil)
            {
                baseType = GetTypeName(typeDefinition.BaseType, metadataReader);
            }

            var interfaces = typeDefinition.GetInterfaceImplementations()
                .Select(metadataReader.GetInterfaceImplementation)
                .Select(ii => GetTypeName(ii.Interface, metadataReader))
                .Where(n => n != null)
                .Cast<string>()
                .ToList();

            bool isNested = typeDefinition.IsNested;
            string? declaringType = null;
            if (isNested && !typeDefinition.GetDeclaringType().IsNil)
            {
                var declaringTypeDef = metadataReader.GetTypeDefinition(typeDefinition.GetDeclaringType());
                string declaringTypeName = metadataReader.GetString(declaringTypeDef.Name);
                string? declaringTypeNamespace = declaringTypeDef.Namespace.IsNil ? null : metadataReader.GetString(declaringTypeDef.Namespace);
                declaringType = string.IsNullOrEmpty(declaringTypeNamespace)
                    ? declaringTypeName
                    : $"{declaringTypeNamespace}.{declaringTypeName}";
            }

            return new TypeInfo
            {
                Name = name,
                FullName = fullName,
                Namespace = namespaceName,
                Assembly = assembly,
                Kind = typeKind,
                Accessibility = accessibility,
                IsAbstract = (attributes & TypeAttributes.Abstract) != 0 && typeKind != TypeKind.Interface,
                IsSealed = (attributes & TypeAttributes.Sealed) != 0 && typeKind != TypeKind.Enum,
                IsStatic = (attributes & TypeAttributes.Abstract) != 0 && (attributes & TypeAttributes.Sealed) != 0,
                IsGeneric = genericParams.Count > 0,
                GenericParameterCount = genericParams.Count,
                GenericParameters = genericParams,
                BaseType = baseType,
                Interfaces = interfaces,
                IsNested = isNested,
                DeclaringType = declaringType,
                MetadataToken = metadataReader.GetToken(typeDefinitionHandle)
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error creating type info");
            return null;
        }
    }

    private static TypeKind DetermineTypeKind(TypeDefinition typeDefinition, MetadataReader metadataReader)
    {
        var attributes = typeDefinition.Attributes;
        if ((attributes & TypeAttributes.Interface) != 0)
            return TypeKind.Interface;

        if (!typeDefinition.BaseType.IsNil)
        {
            string? baseTypeName = GetTypeName(typeDefinition.BaseType, metadataReader);
            switch (baseTypeName)
            {
                case "System.Enum":
                    return TypeKind.Enum;
                case "System.MulticastDelegate":
                case "System.Delegate":
                    return TypeKind.Delegate;
            }
        }

        if (((attributes & TypeAttributes.SequentialLayout) != 0 || (attributes & TypeAttributes.ExplicitLayout) != 0) && !typeDefinition.BaseType.IsNil)
        {
            string? baseTypeName = GetTypeName(typeDefinition.BaseType, metadataReader);
            if (baseTypeName == "System.ValueType")
                return TypeKind.Struct;
        }

        return TypeKind.Class;
    }

    private static TypeAccessibility DetermineAccessibility(TypeAttributes attributes)
    {
        var visibility = attributes & TypeAttributes.VisibilityMask;
        return visibility switch
        {
            TypeAttributes.Public or TypeAttributes.NestedPublic => TypeAccessibility.Public,
            TypeAttributes.NotPublic or TypeAttributes.NestedAssembly => TypeAccessibility.Internal,
            TypeAttributes.NestedPrivate => TypeAccessibility.Private,
            TypeAttributes.NestedFamily => TypeAccessibility.Protected,
            TypeAttributes.NestedFamORAssem => TypeAccessibility.ProtectedInternal,
            TypeAttributes.NestedFamANDAssem => TypeAccessibility.PrivateProtected,
            _ => TypeAccessibility.Unknown
        };
    }

    private static string? GetTypeName(EntityHandle handle, MetadataReader metadataReader)
    {
        try
        {
            if (handle.IsNil)
                return null;

            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    var typeDef = metadataReader.GetTypeDefinition((TypeDefinitionHandle)handle);
                    string name = metadataReader.GetString(typeDef.Name);
                    string? ns = typeDef.Namespace.IsNil ? null : metadataReader.GetString(typeDef.Namespace);
                    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

                case HandleKind.TypeReference:
                    var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)handle);
                    string refName = metadataReader.GetString(typeRef.Name);
                    string? refNs = typeRef.Namespace.IsNil ? null : metadataReader.GetString(typeRef.Namespace);
                    return string.IsNullOrEmpty(refNs) ? refName : $"{refNs}.{refName}";

                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCompilerGenerated(string typeName) =>
        typeName.StartsWith('<') || typeName.Contains("<>") || typeName.StartsWith("__") || typeName == "<Module>";

    private static Regex WildcardToRegex(string pattern)
    {
        string escaped = Regex.Escape(pattern);
        escaped = escaped.Replace("\\*", ".*").Replace("\\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string GetCacheKey(string normalizedPath)
    {
        return $"{CacheKeyPrefix}{normalizedPath}";
    }
}
