using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Index
{
    public sealed class IndexDocument
    {
        // last id per shortType
        public Dictionary<string, int> LastAssigned { get; init; } = new()
        { ["c"]=0, ["p"]=0, ["o"]=0, ["t"]=0, ["sr"]=0, ["tc"]=0, ["l"]=0 };

        public List<IndexItem> Items { get; set; } = [];
    }
}
