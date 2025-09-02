using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ThreatFramework.IndexBuilder; // requires project reference

namespace ThreatFramework.Infrastructure.Cache;

/// <summary>
/// Thread-safe in-memory cache mapping (kind,guid) -> sequential Id derived from index build.
/// </summary>
public sealed class IndexCache : IIndexCache
{
    private readonly IIndexBuilder _builder;
    private readonly ILogger<IndexCache> _logger;
    private readonly ConcurrentDictionary<(string kind, Guid guid), long> _map = new();
    private long _version;
    private volatile bool _initialized;
    private readonly SemaphoreSlim _refreshLock = new(1,1);

    public IndexCache(IIndexBuilder builder, ILogger<IndexCache> logger)
    {
        _builder = builder;
        _logger = logger;
    }

    public long Version => Interlocked.Read(ref _version);

    public bool TryGetId(string kind, Guid guid, out long id)
    {
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Kind required", nameof(kind));
        EnsureInitialized();
        return _map.TryGetValue(Normalize(kind, guid), out id);
    }

    public long GetId(string kind, Guid guid)
    {
        if (TryGetId(kind, guid, out var id)) return id;
        throw new KeyNotFoundException($"No entry for kind='{kind}' guid='{guid}'");
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("Refreshing index cache...");
            var doc = await _builder.BuildAsync(ct).ConfigureAwait(false);
            var temp = new ConcurrentDictionary<(string, Guid), long>();
            foreach (var item in doc.Items)
            {
                if (item.Guid == Guid.Empty) continue; // skip empty (e.g., propertyOption without guid)
                temp[Normalize(item.Kind, item.Guid)] = item.Id;
            }
            // Swap
            _map.Clear();
            foreach (var kv in temp)
                _map[kv.Key] = kv.Value;
            Interlocked.Increment(ref _version);
            _initialized = true;
            _logger.LogInformation("Index cache refresh complete: {Count} entries, version {Version}", _map.Count, Version);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        // Fire and forget initial load (caller thread waits) to avoid multiple concurrent builds
        RefreshAsync().GetAwaiter().GetResult();
    }

    private static (string, Guid) Normalize(string kind, Guid guid) => (kind.Trim().ToLowerInvariant(), guid);
}
