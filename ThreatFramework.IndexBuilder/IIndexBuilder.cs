namespace ThreatFramework.IndexBuilder;

public interface IIndexBuilder
{
    Task<IndexDocument> BuildAsync(CancellationToken ct = default);
}
