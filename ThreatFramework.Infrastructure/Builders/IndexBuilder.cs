using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.Core.Domain;

namespace ThreatFramework.Infrastructure.Builders
{
    public sealed class IndexBuilder(
            IDatabaseReader db,
            IIndexStore store,
            IIdAllocator allocator,
            IUuid5Generator uuid5) : IIndexBuilder
    {
        /*
         PSEUDOCODE / PLAN
         - If rebuild flag set: overwrite store with empty IndexDocument
         - Load store (before count)
         - Track 'added' count
         - Local function AddAll(kind, query):
             * execute query to get list of (Guid guid, string name)
             * for each tuple:
                 - capture count before
                 - call allocator.GetOrAssignAsync
                 - if count increased -> increment added
         - Call AddAll for each entity type (Component, Property, Threat, SecurityRequirement, TestCase, Library)
         - Handle PropertyOptions:
             * db.GetPropertyOptionsAsync returns tuples (Guid? Guid, string OptionText)
             * Previous code assumed (Guid? optGuid, Guid propGuid, string text) causing CS8132 & CS8130
             * Deconstruct into two variables: (optGuid, text)
             * If optGuid is null -> deterministically derive GUID using namespace + normalized text
               (property guid not available in current signature; fall back to text-only)
             * Perform same added-count logic as above
         - Reload final document and return (added, total count, store path)
         - Normalize helper: lowercase, trim, collapse whitespace
        */
        public async Task<(int added, int total, string path)> BuildOrUpdateAsync(bool rebuild, CancellationToken ct)
        {
            if (rebuild)
                await store.SaveAsync(new(), ct);

            var before = (await store.LoadAsync(ct)).Items.Count;
            var added = 0;

            async Task AddAll(EntityKind k, Func<CancellationToken, Task<IReadOnlyList<(Guid Guid, string Name)>>> query)
            {
                foreach (var (guid, name) in await query(ct))
                {
                    var pre = (await store.LoadAsync(ct)).Items.Count;
                    _ = await allocator.GetOrAssignAsync(k, guid, name ?? string.Empty, ct);
                    if ((await store.LoadAsync(ct)).Items.Count > pre)
                        added++;
                }
            }

            await AddAll(EntityKind.Component, db.GetComponentsAsync);
            await AddAll(EntityKind.Property, db.GetPropertiesAsync);
            await AddAll(EntityKind.Threat, db.GetThreatsAsync);    
            await AddAll(EntityKind.SecurityRequirement, db.GetSecurityRequirementsAsync);
            await AddAll(EntityKind.TestCase, db.GetTestCasesAsync);
            await AddAll(EntityKind.Library, db.GetLibrariesAsync);

            // PropertyOptions: Guid may be null -> deterministic pseudo-guid (fallback uses normalized text)
            var options = await db.GetPropertyOptionsAsync(ct);
            foreach (var (optGuid, text) in options)
            {
                var guid = optGuid ?? uuid5.FromNamespaceAndName(
                    Uuid5Generator.NamespacePropertyOption,
                    Normalize(text));

                var pre = (await store.LoadAsync(ct)).Items.Count;
                _ = await allocator.GetOrAssignAsync(EntityKind.PropertyOption, guid, text ?? string.Empty, ct);
                if ((await store.LoadAsync(ct)).Items.Count > pre)
                    added++;
            }

            var doc = await store.LoadAsync(ct);
            return (added, doc.Items.Count, store.Path);
        }

        private static string Normalize(string s)
            => Regex.Replace((s ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", " ");
    }
}
