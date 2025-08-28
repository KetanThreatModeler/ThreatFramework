using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Index;

namespace ThreatFramework.Core.Abstractions
{
    public interface IIndexStore
    {
        Task<IndexDocument> LoadAsync(CancellationToken ct);
        Task SaveAsync(IndexDocument doc, CancellationToken ct);
        string Path { get; }
    }
}
