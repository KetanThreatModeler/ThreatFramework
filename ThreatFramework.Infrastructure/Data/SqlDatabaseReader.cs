using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.Infrastructure.Options;

namespace ThreatFramework.Infrastructure.Data
{
    public sealed class SqlDatabaseReader(IOptions<DatabaseOptions> db) : IDatabaseReader
    {
        private string Conn => db.Value.GoldenDb;

        // Enumerate allowed entity tables (prevents arbitrary injection of table names).
        private enum EntityTable
        {
            Components,
            Properties,
            Threats,
            SecurityRequirements,
            TestCases,
            Libraries
        }

        // Central map from enum to actual table names (decouples naming if schema changes).
        private static readonly IReadOnlyDictionary<EntityTable, string> TableNames = new Dictionary<EntityTable, string>
        {
            { EntityTable.Components, "Components" },
            { EntityTable.Properties, "Properties" },
            { EntityTable.Threats, "Threats" },
            { EntityTable.SecurityRequirements, "SecurityRequirements" },
            { EntityTable.TestCases, "TestCases" },
            { EntityTable.Libraries, "Libraries" }
        };

        // Very small query builder specialized for simple SELECT patterns.
        private static class QueryBuilder
        {
            public static string GuidName(string table, bool filterNonNullGuid = true)
            {
                // We only interpolate table names coming from a whitelist dictionary.
                var sb = new StringBuilder(64);
                sb.Append("SELECT [Guid], [Name] FROM [").Append(table).Append(']');
                if (filterNonNullGuid)
                    sb.Append(" WHERE [Guid] IS NOT NULL");
                return sb.ToString();
            }

            public static string PropertyOptions() => "SELECT [Guid], [OptionText] FROM [PropertyOptions]";
        }

        // Shared reader for (Guid, string) pairs.
        private static async Task<IReadOnlyList<(Guid Guid, string Name)>> ReadGuidNameAsync(
            string sql, string connStr, CancellationToken ct)
        {
            const int GuidOrdinal = 0;
            const int NameOrdinal = 1;

            var list = new List<(Guid, string)>(64);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, ct).ConfigureAwait(false);

            while (await rdr.ReadAsync(ct).ConfigureAwait(false))
            {
                var g = rdr.GetGuid(GuidOrdinal);
                var n = rdr.IsDBNull(NameOrdinal) ? string.Empty : rdr.GetString(NameOrdinal);
                list.Add((g, n));
            }
            return list;
        }

        private Task<IReadOnlyList<(Guid Guid, string Name)>> ReadAsync(EntityTable table, CancellationToken ct) =>
            ReadGuidNameAsync(QueryBuilder.GuidName(TableNames[table]), Conn, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetComponentsAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.Components, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetPropertiesAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.Properties, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetThreatsAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.Threats, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetSecurityRequirementsAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.SecurityRequirements, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetTestCasesAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.TestCases, ct);

        public Task<IReadOnlyList<(Guid Guid, string Name)>> GetLibrariesAsync(CancellationToken ct) =>
            ReadAsync(EntityTable.Libraries, ct);

        public async Task<IReadOnlyList<(Guid? Guid, string OptionText)>> GetPropertyOptionsAsync(CancellationToken ct)
        {
                const int GuidOrdinal = 0;
            const int TextOrdinal = 1;
            var sql = QueryBuilder.PropertyOptions();
            var list = new List<(Guid?, string)>(64);

            await using var conn = new SqlConnection(Conn);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, ct).ConfigureAwait(false);

            while (await rdr.ReadAsync(ct).ConfigureAwait(false))
            {
                Guid? g = rdr.IsDBNull(GuidOrdinal) ? null : rdr.GetGuid(GuidOrdinal);
                var text = rdr.IsDBNull(TextOrdinal) ? string.Empty : rdr.GetString(TextOrdinal);
                list.Add((g, text));
            }

            return list;
        }
    }
}
