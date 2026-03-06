using System.Collections.Concurrent;

namespace Oras.Tui;

/// <summary>
/// In-memory cache for TUI data with TTL expiration.
/// </summary>
internal class TuiCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;

    public TuiCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5);
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null)
    {
        var expiration = DateTimeOffset.UtcNow + (ttl ?? _defaultTtl);
        _cache[key] = new CacheEntry
        {
            Value = value!,
            ExpiresAt = expiration
        };
    }

    public (T? Value, bool Found, bool FromCache) Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTimeOffset.UtcNow < entry.ExpiresAt)
            {
                return ((T?)entry.Value, true, true);
            }
            else
            {
                // Expired — remove from cache
                _cache.TryRemove(key, out _);
            }
        }
        return (default, false, false);
    }

    public void Invalidate(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void InvalidatePattern(string pattern)
    {
        var keys = _cache.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
