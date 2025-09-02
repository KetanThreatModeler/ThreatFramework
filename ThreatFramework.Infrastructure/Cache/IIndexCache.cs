namespace ThreatFramework.Infrastructure.Cache;

/// <summary>
/// In-memory lookup cache built from the logical index (index.yaml) providing O(1) Guid+Kind => Id resolution.
/// </summary>
public interface IIndexCache
{
    /// <summary>Refreshes the cache by rebuilding the index from the data source.</summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>Attempts to resolve the numeric Id (per-kind sequence) for the supplied kind + guid.</summary>
    bool TryGetId(string kind, Guid guid, out long id);

    /// <summary>Gets Id or throws KeyNotFoundException when not present.</summary>
    long GetId(string kind, Guid guid);

    /// <summary>Snapshot of the current version incremented every successful refresh.</summary>
    long Version { get; }
}
