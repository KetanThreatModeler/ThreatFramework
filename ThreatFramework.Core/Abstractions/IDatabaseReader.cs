using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ThreatFramework.Core.Abstractions
{

    
    public interface IDatabaseReader
    {
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetComponentsAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetPropertiesAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetThreatsAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetSecurityRequirementsAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetTestCasesAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid Guid, string Name)>> GetLibrariesAsync(CancellationToken ct);
        Task<IReadOnlyList<(Guid? Guid, string OptionText)>> GetPropertyOptionsAsync(CancellationToken ct);
    }
}
