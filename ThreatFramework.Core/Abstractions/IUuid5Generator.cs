using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Abstractions
{
    public interface IUuid5Generator
    {
        Guid FromNamespaceAndName(Guid ns, string name);
    }
}
