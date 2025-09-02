using System.Collections.Concurrent;
using ThreatFramework.Core.Abstractions;

namespace ThreatFramework.IndexBuilder;

public sealed class IndexBuilder(IDatabaseReader reader) : IIndexBuilder
{
    public async Task<IndexDocument> BuildAsync(CancellationToken ct = default)
    {
        var doc = new IndexDocument();
        var counters = new ConcurrentDictionary<string, long>();

        async Task AddRange<T>(string kind, IAsyncEnumerable<(Guid Guid, string Name)> source)
        {
            await foreach (var (guid, name) in source.WithCancellation(ct))
            {
                var id = counters.AddOrUpdate(kind, 1, (_, cur) => cur + 1);
                doc.Items.Add(new IndexItem(kind, guid, id, name));
            }
        }

        await AddRange<(Guid Guid, string Name)>("component", reader.EnumerateComponentsAsync(ct));
        await AddRange<(Guid Guid, string Name)>("property", reader.EnumeratePropertiesAsync(ct));
        await AddRange<(Guid Guid, string Name)>("threat", reader.EnumerateThreatsAsync(ct));
        await AddRange<(Guid Guid, string Name)>("securityRequirement", reader.EnumerateSecurityRequirementsAsync(ct));
        await AddRange<(Guid Guid, string Name)>("testCase", reader.EnumerateTestCasesAsync(ct));
        await AddRange<(Guid Guid, string Name)>("library", reader.EnumerateLibrariesAsync(ct));

        // Property options (Guid nullable)
        long optCounter = 0;
        await foreach (var (optGuid, text) in reader.EnumeratePropertyOptionsAsync(ct).WithCancellation(ct))
        {
            var id = Interlocked.Increment(ref optCounter);
            doc.Items.Add(new IndexItem("propertyOption", optGuid ?? Guid.Empty, id, text));
        }

        // Keep ordering stable
        doc.Items = doc.Items
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.Id)
            .ToList();

        return doc;
    }
}
