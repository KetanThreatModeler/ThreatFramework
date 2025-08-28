using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.Core.Domain;
using ThreatFramework.Core.Index;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class IdAllocator(IIndexStore store) : IIdAllocator
    {
        public async Task<IndexItem> GetOrAssignAsync(EntityKind kind, Guid guid, string name, CancellationToken ct)
        {
            var doc = await store.LoadAsync(ct);
            var existing = doc.Items.FirstOrDefault(i => i.Guid == guid);
            if (existing is not null) return existing;

            var shortType = kind.Short();
            var canonical = kind.Canonical();

            var last = doc.LastAssigned.TryGetValue(shortType, out var v) ? v : 0;
            var next = last + 1;
            doc.LastAssigned[shortType] = next;

            var item = new IndexItem(canonical, shortType, name, guid, next);
            doc.Items.Add(item);
            await store.SaveAsync(doc, ct);
            return item;
        }
    }
}
