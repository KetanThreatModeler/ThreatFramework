namespace ThreatFramework.IndexBuilder;

public sealed record IndexItem(string Kind, Guid Guid, long Id, string Name);

public sealed class IndexDocument
{
    public List<IndexItem> Items { get; set; } = new();
}
