using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Abstractions
{
    public interface IIndexBuilder
    {
        Task<(int added, int total, string path)> BuildOrUpdateAsync(bool rebuild, CancellationToken ct);
    }
}
