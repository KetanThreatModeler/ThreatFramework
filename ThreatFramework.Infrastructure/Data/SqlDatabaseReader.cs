using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.Infrastructure.Options;
using ThreatFramework.SQLQueryBuilder;

namespace ThreatFramework.Infrastructure.Data;

public sealed class SqlDatabaseReader(
    IOptions<DatabaseOptions> db,
    ILogger<SqlDatabaseReader> logger) : IDatabaseReader
{
    private string Conn => db.Value.GoldenDb;

    private async IAsyncEnumerable<(Guid Guid, string Name)> EnumerateGuidNameAsync(string table, string guidCol, string nameCol, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var sql = SelectQueryBuilder
            .From(table)
            .Columns(guidCol, nameCol)
            .OrderBy(nameCol)
            .Build();
        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql.CommandText, conn);
        foreach (var p in sql.Parameters)
            cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var ordGuid = rdr.GetOrdinal(guidCol);
        var ordName = rdr.GetOrdinal(nameCol);
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            if (rdr.IsDBNull(ordGuid)) continue; // skip null guid for main entities
            var g = rdr.GetGuid(ordGuid);
            var n = rdr.IsDBNull(ordName) ? string.Empty : rdr.GetString(ordName);
            yield return (g, n);
        }
    }

    private async IAsyncEnumerable<(Guid? Guid, string OptionText)> EnumerateOptionAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var sql = SelectQueryBuilder
            .From("PropertyOptions")
            .Columns("Guid", "OptionText")
            .OrderBy("OptionText")
            .Build();
        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql.CommandText, conn);
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var og = rdr.GetOrdinal("Guid");
        var ot = rdr.GetOrdinal("OptionText");
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            Guid? g = rdr.IsDBNull(og) ? null : rdr.GetGuid(og);
            var t = rdr.IsDBNull(ot) ? string.Empty : rdr.GetString(ot);
            yield return (g, t);
        }
    }

    public IAsyncEnumerable<(Guid Guid, string Name)> EnumerateComponentsAsync(CancellationToken ct) => EnumerateGuidNameAsync("Components", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid Guid, string Name)> EnumeratePropertiesAsync(CancellationToken ct) => EnumerateGuidNameAsync("Properties", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid Guid, string Name)> EnumerateThreatsAsync(CancellationToken ct) => EnumerateGuidNameAsync("Threats", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid Guid, string Name)> EnumerateSecurityRequirementsAsync(CancellationToken ct) => EnumerateGuidNameAsync("SecurityRequirements", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid Guid, string Name)> EnumerateTestCasesAsync(CancellationToken ct) => EnumerateGuidNameAsync("TestCases", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid Guid, string Name)> EnumerateLibrariesAsync(CancellationToken ct) => EnumerateGuidNameAsync("Libraries", "Guid", "Name", ct);
    public IAsyncEnumerable<(Guid? Guid, string OptionText)> EnumeratePropertyOptionsAsync(CancellationToken ct) => EnumerateOptionAsync(ct);
}
