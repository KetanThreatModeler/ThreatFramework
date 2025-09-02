using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThreatFramework.SQLQueryBuilder;

public enum SortDirection { Asc, Desc }

public enum SqlOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterOrEqual,
    LessThan,
    LessOrEqual,
    Like
}

public sealed record SqlParameterData(string Name, object? Value, DbType? DbType);

public sealed record BuiltSql(string CommandText, IReadOnlyList<SqlParameterData> Parameters);

public sealed class SelectQueryBuilder
{
    private static readonly Regex IdentifierRx = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly List<string> _columns = new();
    private readonly List<(string clause, string? logic)> _where = new();
    private readonly List<string> _order = new();
    private readonly List<SqlParameterData> _parameters = new();

    private string? _table;
    private bool _distinct;
    private int? _top;
    private bool _withNoLock;
    private int? _offset;
    private int? _fetch;

    private SelectQueryBuilder() { }

    public static SelectQueryBuilder From(string table)
    {
        var b = new SelectQueryBuilder();
        b.Table(table);
        return b;
    }

    public SelectQueryBuilder Table(string table)
    {
        _table = SanitizeIdentifier(table, nameof(table));
        return this;
    }

    public SelectQueryBuilder Distinct()
    {
        _distinct = true;
        return this;
    }

    public SelectQueryBuilder Top(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        _top = count;
        return this;
    }

    public SelectQueryBuilder NoLock()
    {
        _withNoLock = true;
        return this;
    }

    public SelectQueryBuilder Columns(params string[] columns)
    {
        if (columns is null) throw new ArgumentNullException(nameof(columns));
        foreach (var c in columns.Where(c => !string.IsNullOrWhiteSpace(c)))
            _columns.Add(Bracket(SanitizeIdentifier(c.Trim(), "column")));
        return this;
    }

    public SelectQueryBuilder Where(string column, SqlOperator op, object? value, DbType? dbType = null, bool or = false)
    {
        var logic = _where.Count == 0 ? null : (or ? "OR" : "AND");
        var col = Bracket(SanitizeIdentifier(column, nameof(column)));
        var (token, parameterValue) = OperatorToSql(op, value);
        var pName = NextParameterName();
        _parameters.Add(new SqlParameterData(pName, parameterValue, dbType));
        _where.Add(($"{col} {token} @{pName}", logic));
        return this;
    }

    public SelectQueryBuilder WhereIn<T>(string column, IEnumerable<T> values, bool or = false)
    {
        var vals = values?.ToList() ?? throw new ArgumentNullException(nameof(values));
        if (vals.Count == 0) throw new ArgumentException("IN list cannot be empty.", nameof(values));
        var logic = _where.Count == 0 ? null : (or ? "OR" : "AND");
        var col = Bracket(SanitizeIdentifier(column, nameof(column)));
        var paramNames = new List<string>(vals.Count);
        foreach (var v in vals)
        {
            var p = NextParameterName();
            _parameters.Add(new SqlParameterData(p, v, null));
            paramNames.Add("@" + p);
        }
        _where.Add(($"{col} IN ({string.Join(",", paramNames)})", logic));
        return this;
    }

    public SelectQueryBuilder WhereRaw(string rawPredicate, bool or = false)
    {
        if (string.IsNullOrWhiteSpace(rawPredicate)) throw new ArgumentException("Raw predicate required.", nameof(rawPredicate));
        var logic = _where.Count == 0 ? null : (or ? "OR" : "AND");
        _where.Add((rawPredicate, logic));
        return this;
    }

    public SelectQueryBuilder OrderBy(string column, SortDirection dir = SortDirection.Asc)
    {
        var col = Bracket(SanitizeIdentifier(column, nameof(column)));
        _order.Add($"{col} {(dir == SortDirection.Asc ? "ASC" : "DESC")}");
        return this;
    }

    public SelectQueryBuilder Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        _offset = (pageNumber - 1) * pageSize;
        _fetch = pageSize;
        return this;
    }

    public BuiltSql Build()
    {
        if (_table is null) throw new InvalidOperationException("Table not specified.");
        if (_fetch.HasValue && !_order.Any())
            throw new InvalidOperationException("ORDER BY required when using pagination (OFFSET/FETCH).");

        var sb = new StringBuilder(256);
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");
        if (_top.HasValue) sb.Append("TOP ").Append(_top.Value).Append(' ');
        sb.Append(_columns.Count == 0 ? "*" : string.Join(",", _columns));
        sb.Append(" FROM ").Append(Bracket(_table));
        if (_withNoLock) sb.Append(" WITH (NOLOCK)");

        if (_where.Count > 0)
        {
            sb.Append(" WHERE ");
            bool first = true;
            foreach (var (clause, logic) in _where)
            {
                if (!first) sb.Append(' ').Append(logic).Append(' ');
                sb.Append(clause);
                first = false;
            }
        }

        if (_order.Count > 0)
        {
            sb.Append(" ORDER BY ").Append(string.Join(", ", _order));
        }

        if (_offset.HasValue && _fetch.HasValue)
        {
            sb.Append(" OFFSET ").Append(_offset.Value).Append(" ROWS FETCH NEXT ").Append(_fetch.Value).Append(" ROWS ONLY");
        }

        return new BuiltSql(sb.ToString(), _parameters.AsReadOnly());
    }

    private static (string token, object? value) OperatorToSql(SqlOperator op, object? value) =>
        op switch
        {
            SqlOperator.Equal => ("=", value),
            SqlOperator.NotEqual => ("<>", value),
            SqlOperator.GreaterThan => (">", value),
            SqlOperator.GreaterOrEqual => (">=", value),
            SqlOperator.LessThan => ("<", value),
            SqlOperator.LessOrEqual => ("<=", value),
            SqlOperator.Like => ("LIKE", value),
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

    private string NextParameterName() => $"p{_parameters.Count}";

    private static string SanitizeIdentifier(string raw, string paramName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Identifier cannot be empty.", paramName);
        if (!IdentifierRx.IsMatch(raw))
            throw new ArgumentException($"Invalid identifier '{raw}'. Allowed: letters, digits, underscore; cannot start with digit.", paramName);
        return raw;
    }

    private static string Bracket(string ident) => $"[{ident}]";
}