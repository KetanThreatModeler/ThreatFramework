namespace ThreatFramework.IndexBuilder
{
    // Prefer a mutable POCO for defensive deserialization.
    public sealed class IndexItem
    {
        public IndexItem(string kind, Guid guid, long id, string name)
        {
            Kind = kind;
            Guid = guid;
            Id = id;
            Name = name;
        }

        public string Kind { get; set; } = "";
        public Guid Guid { get; set; }
        public long Id { get; set; }
        public string? Name { get; set; }
    }

    public sealed class IndexDocument
    {
        public List<IndexItem> Items { get; set; } = new();
    }
}
