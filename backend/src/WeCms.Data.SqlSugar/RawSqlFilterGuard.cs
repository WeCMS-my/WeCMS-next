using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WeCms.Data.SqlSugar;

public static class RawSqlFilterGuard
{
    private static readonly Regex TenantFilterPattern = new(
        @"\btenant_id\s*(?:=|IN)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DataScopeFilterPattern = new(
        @"\bcreated_by_user_id\s*(?:=|IN)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeletedAtFilterPattern = new(
        @"\bdeleted_at\s+IS\s+NULL\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SoftDeleteTablePattern = new(
        @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE\s+FROM)\s+([A-Za-z_][A-Za-z0-9_]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StatementTypePattern = new(
        @"^\s*(SELECT|UPDATE|DELETE|INSERT)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void RequireDataBoundaryFilters(
        string sql,
        string operation,
        QueryFilterContext context,
        IReadOnlySet<string> softDeleteTables,
        IReadOnlySet<string> tenantTables,
        IReadOnlySet<string> dataScopeTables)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        RequireDeletedAtFilter(sql, operation, softDeleteTables);

        if (!ShouldGuardStatement(sql, out var statementType))
        {
            return;
        }

        if (statementType is "INSERT")
        {
            return;
        }

        var affectedTables = ExtractTables(sql);
        if (affectedTables.Count == 0)
        {
            return;
        }

        if (context.TenantId is not null && AffectedTableMatches(affectedTables, tenantTables)
            && !TenantFilterPattern.IsMatch(sql))
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit tenant_id predicate when querying tenant-scoped tables.\nSQL: {TrimSql(sql)}");
        }

        if (context.DataScopeUserIds.Count > 0 && AffectedTableMatches(affectedTables, dataScopeTables)
            && !DataScopeFilterPattern.IsMatch(sql))
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit created_by_user_id predicate when querying data-scoped tables.\nSQL: {TrimSql(sql)}");
        }
    }

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
        foreach (var match in ExtractTables(sql))
        {
            if (softDeleteTables.Contains(match))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AffectedTableMatches(IReadOnlySet<string> tables, IReadOnlySet<string> targetTables)
    {
        foreach (var table in tables)
        {
            if (targetTables.Contains(table))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlySet<string> ExtractTables(string sql)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var match in SoftDeleteTablePattern.Matches(sql).Select(static match => match.Groups[1].Value))
        {
            if (!string.IsNullOrWhiteSpace(match))
            {
                tables.Add(match);
            }
        }

        return tables;
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
