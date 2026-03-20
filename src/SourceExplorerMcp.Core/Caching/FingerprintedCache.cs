using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;

namespace SourceExplorerMcp.Core.Caching;

internal sealed record FingerprintedCache<T>(T Value, long Fingerprint) where T : notnull;

internal static class FingerprintedCacheExtensions
{
    extension(IMemoryCache cache)
    {
        public bool TryGetValid<T>(string key, long fingerprint, [NotNullWhen(true)] out T? value)
            where T : notnull
        {
            if (cache.TryGetValue<FingerprintedCache<T>>(key, out var entry)
                && entry is not null
                && entry.Fingerprint == fingerprint)
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        public void SetWithFingerprint<T>(string key, T value, long fingerprint)
            where T : notnull
        {
            cache.Set(key, new FingerprintedCache<T>(value, fingerprint));
        }
    }
}
