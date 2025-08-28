using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Domain;
using ThreatFramework.Core.Index;

namespace ThreatFramework.Core.Abstractions
{
    public interface IIdAllocator
    {
        Task<IndexItem> GetOrAssignAsync(EntityKind kind, Guid guid, string name, CancellationToken ct);
    }
}
