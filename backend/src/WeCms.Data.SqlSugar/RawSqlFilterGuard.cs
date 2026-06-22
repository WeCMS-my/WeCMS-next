using System.Text.RegularExpressions;

using System.Collections.Generic;
using System.Linq;

namespace WeCms.Data.SqlSugar;

public static class RawSqlFilterGuard
{
    private static readonly Regex DeletedAtFilterPattern = new(
        @"\bdeleted_at\s+IS\s+NULL\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SoftDeleteTablePattern = new(
        @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE\s+FROM)\s+([A-Za-z_][A-Za-z0-9_]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StatementTypePattern = new(
        @"^\s*(SELECT|UPDATE|DELETE|INSERT)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void RequireDeletedAtFilter(
        string sql,
        string operation,
        IReadOnlySet<string> softDeleteTables)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL text is required.", nameof(sql));
        }

        ArgumentNullException.ThrowIfNull(softDeleteTables);

        if (!ShouldGuardStatement(sql, out var statementType))
        {
            return;
        }

        if (statementType is "INSERT")
        {
            return;
        }

        if (RequiresDeletedAtFilter(sql, softDeleteTables) && !DeletedAtFilterPattern.IsMatch(sql))
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit deleted_at predicate when querying soft-delete tables.\nSQL: {TrimSql(sql)}");
        }
    }

    public static void RequireDeletedAtFilter(string sql, string operation)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL text is required.", nameof(sql));
        }

        if (!ShouldGuardStatement(sql, out var statementType))
        {
            return;
        }

        if (statementType is "INSERT")
        {
            return;
        }

        if (!DeletedAtFilterPattern.IsMatch(sql))
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit deleted_at predicate.\nSQL: {TrimSql(sql)}");
        }
    }

    private static bool RequiresDeletedAtFilter(string sql, IReadOnlySet<string> softDeleteTables)
    {
        foreach (var match in SoftDeleteTablePattern.Matches(sql).Select(static match => match.Groups[1].Value))
        {
            if (softDeleteTables.Contains(match))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldGuardStatement(string sql, out string statementType)
    {
        var match = StatementTypePattern.Match(sql);
        if (!match.Success)
        {
            statementType = string.Empty;
            return false;
        }

        statementType = match.Groups[1].Value.ToUpperInvariant();
        return statementType is "SELECT" or "UPDATE" or "DELETE" or "INSERT";
    }

    private static string TrimSql(string sql)
    {
        return sql.Replace('\n', ' ').Trim();
    }
}
