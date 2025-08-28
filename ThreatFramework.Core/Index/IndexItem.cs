using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Index
{
    public sealed record IndexItem(
    string Type,        // canonical: "threat"
    string ShortType,   // "t"
    string Name,
    Guid Guid,
    long Id
);
}
