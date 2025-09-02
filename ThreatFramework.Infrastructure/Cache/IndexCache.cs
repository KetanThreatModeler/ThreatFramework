using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ThreatFramework.IndexBuilder; // requires project reference
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

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
            _logger.LogInformation("Refreshing index cache from database...");
            var doc = await _builder.BuildAsync(ct).ConfigureAwait(false);
            Populate(doc);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task RefreshFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required", nameof(filePath));
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Index file not found", filePath);
            _logger.LogInformation("Refreshing index cache from file {File}", filePath);
            var yaml = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var doc = Deserializer.Deserialize<IndexDocument>(yaml) ?? throw new InvalidOperationException("Failed to deserialize index document");
            Populate(doc);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void Populate(IndexDocument doc)
    {
        var temp = new ConcurrentDictionary<(string, Guid), long>();
        foreach (var item in doc.Items)
        {
            if (item.Guid == Guid.Empty) continue;
            temp[Normalize(item.Kind, item.Guid)] = item.Id;
        }
        _map.Clear();
        foreach (var kv in temp) _map[kv.Key] = kv.Value;
        Interlocked.Increment(ref _version);
        _initialized = true;
        _logger.LogInformation("Index cache populated: {Count} entries version {Version}", _map.Count, Version);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        RefreshAsync().GetAwaiter().GetResult();
    }

    private static (string, Guid) Normalize(string kind, Guid guid) => (kind.Trim().ToLowerInvariant(), guid);
}
