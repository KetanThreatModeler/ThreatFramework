using System.Collections;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ThreatFramework.Infrastructure.Data.Sql;

/// <summary>
/// Lightweight, internal fluent SELECT builder with table / column whitelisting and automatic parameterization.
/// Not intended as a general ORM – focused on simple, safe read scenarios.
/// </summary>
internal sealed class SelectBuilder
{
    private readonly string _table;
    private readonly HashSet<string> _columns = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _orderBy = [];
    private readonly List<ICondition> _conditions = [];
    private bool _distinct;
    private int? _top;

    // Parameter state
    private readonly List<SqlParameter> _parameters = [];
    private int _paramIndex;

    // Whitelisted schema: table -> allowed columns
    private static readonly Dictionary<string, HashSet<string>> Schemas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Components"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["Properties"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["Threats"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["SecurityRequirements"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["TestCases"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["Libraries"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "Name" },
        ["PropertyOptions"] = new(StringComparer.OrdinalIgnoreCase) { "Guid", "OptionText" }
    };

    private SelectBuilder(string table)
    {
        _table = ValidateTable(table);
    }

    public static SelectBuilder From(string table) => new(table);

    public SelectBuilder Distinct()
    {
        _distinct = true;
        return this;
    }

    public SelectBuilder Top(int n)
    {
        if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
        _top = n;
        return this;
    }

    public SelectBuilder WithColumns(params string[] columns)
    {
        if (columns is null || columns.Length == 0)
            throw new ArgumentException("At least one column required.", nameof(columns));

        foreach (var c in columns)
        {
            var col = ValidateColumn(_table, c);
            _columns.Add(col);
        }
        return this;
    }

    public SelectBuilder Where(string column, string op, object? value)
    {
        column = ValidateColumn(_table, column);
        op = NormalizeOperator(op);
        if (value is IEnumerable ie && value is not string && op.Equals("IN", StringComparison.OrdinalIgnoreCase))
        {
            var vals = ie.Cast<object?>().ToArray();
            if (vals.Length == 0)
                // Empty IN list is always false – simplify to 1=0
                _conditions.Add(new RawCondition("1=0"));
            else
                _conditions.Add(new InCondition(column, vals));
        }
        else
        {
            if (op.Equals("IN", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Use an IEnumerable value with IN operator.");
            _conditions.Add(new ComparisonCondition(column, op, value));
        }
        return this;
    }

    public SelectBuilder WhereNotNull(string column)
    {
        column = ValidateColumn(_table, column);
        _conditions.Add(new RawCondition($"[{column}] IS NOT NULL"));
        return this;
    }

    public SelectBuilder OrderBy(string column, bool descending = false)
    {
        column = ValidateColumn(_table, column);
        _orderBy.Add($"[{column}]" + (descending ? " DESC" : " ASC"));
        return this;
    }

    public BuildResult Build()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("No columns specified. Call WithColumns().");

        var sb = new StringBuilder(128);
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");
        if (_top.HasValue) sb.Append("TOP (").Append(_top.Value).Append(") ");
        sb.Append(string.Join(", ", _columns.Select(c => $"[{c}]")));
        sb.Append(" FROM [").Append(_table).Append(']');

        if (_conditions.Count > 0)
        {
            sb.Append(" WHERE ");
            bool first = true;
            foreach (var cond in _conditions)
            {
                if (!first) sb.Append(" AND ");
                cond.AppendSql(this, sb);
                first = false;
            }
        }

        if (_orderBy.Count > 0)
        {
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));
        }

        return new BuildResult(sb.ToString(), _parameters);
    }

    // Internal: add parameter
    private string AddParameter(object? value)
    {
        string name = "@p" + _paramIndex++;
        var p = new SqlParameter(name, value ?? DBNull.Value);
        _parameters.Add(p);
        return name;
    }

    private static string ValidateTable(string table)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table name required.", nameof(table));
        if (!Schemas.ContainsKey(table))
            throw new InvalidOperationException($"Table '{table}' not registered in schema whitelist.");
        return table;
    }

    private static string ValidateColumn(string table, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Column name required.", nameof(column));
        if (!Schemas.TryGetValue(table, out var cols) || !cols.Contains(column))
            throw new InvalidOperationException($"Column '{column}' not allowed for table '{table}'.");
        return column;
    }

    private static string NormalizeOperator(string op)
    {
        op = op?.Trim().ToUpperInvariant() ?? throw new ArgumentNullException(nameof(op));
        return op switch
        {
            "=" or "<>" or ">" or "<" or ">=" or "<=" or "LIKE" or "IN" => op,
            _ => throw new NotSupportedException($"Operator '{op}' is not supported.")
        };
    }

    // Condition abstractions
    private interface ICondition
    {
        void AppendSql(SelectBuilder b, StringBuilder sb);
    }

    private sealed class RawCondition(string sql) : ICondition
    {
        public void AppendSql(SelectBuilder b, StringBuilder sb) => sb.Append(sql);
    }

    private sealed class ComparisonCondition(string column, string op, object? value) : ICondition
    {
        public void AppendSql(SelectBuilder b, StringBuilder sb)
        {
            string param = b.AddParameter(value);
            sb.Append('[').Append(column).Append("] ").Append(op).Append(' ').Append(param);
        }
    }

    private sealed class InCondition(string column, object?[] values) : ICondition
    {
        public void AppendSql(SelectBuilder b, StringBuilder sb)
        {
            var paramNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                paramNames[i] = b.AddParameter(values[i]);
            sb.Append('[').Append(column).Append("] IN (").Append(string.Join(", ", paramNames)).Append(')');
        }
    }

    public readonly record struct BuildResult(string Sql, IReadOnlyList<SqlParameter> Parameters);
}