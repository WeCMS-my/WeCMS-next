using System.Collections.Generic;
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
    private static readonly Regex TableReferencePattern = new(
        @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE\s+FROM)\s+(?<table>[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?:AS\s+)?(?<alias>[A-Za-z_][A-Za-z0-9_]*))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StatementTypePattern = new(
        @"^\s*(SELECT|UPDATE|DELETE|INSERT|WITH)\b",
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

        var affectedTables = ExtractTableReferences(sql);
        if (affectedTables.Count == 0)
        {
            return;
        }

        var missingTenantReference = context.TenantId is not null
            ? FirstMissingReference(sql, affectedTables, tenantTables, "tenant_id", "@tenantId", TenantFilterPattern)
            : null;
        if (missingTenantReference is not null)
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit {missingTenantReference.ExpectedPredicate} predicate when querying tenant-scoped tables.\nSQL: {TrimSql(sql)}");
        }

        var missingDataScopeReference = context.DataScopeUserIds.Count > 0
            ? FirstMissingReference(sql, affectedTables, dataScopeTables, "created_by_user_id", "@dataScopeUserIds", DataScopeFilterPattern)
            : null;
        if (missingDataScopeReference is not null)
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit {missingDataScopeReference.ExpectedPredicate} predicate when querying data-scoped tables.\nSQL: {TrimSql(sql)}");
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

        var missingReference = FirstMissingSoftDeleteReference(sql, ExtractTableReferences(sql), softDeleteTables);
        if (missingReference is not null)
        {
            throw new InvalidOperationException(
                $"Raw SQL operation '{operation}' in {nameof(RawSqlFilterGuard)} requires an explicit {missingReference.ExpectedPredicate} predicate when querying soft-delete tables.\nSQL: {TrimSql(sql)}");
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

    private static MissingReference? FirstMissingReference(
        string sql,
        IReadOnlyList<TableReference> references,
        IReadOnlySet<string> targetTables,
        string column,
        string parameter,
        Regex fallbackPattern)
    {
        foreach (var reference in references)
        {
            if (!targetTables.Contains(reference.Table))
            {
                continue;
            }

            if (!reference.HasExplicitAlias)
            {
                if (!fallbackPattern.IsMatch(sql))
                {
                    return new MissingReference($"{column} predicate");
                }

                continue;
            }

            if (!HasQualifiedPredicate(sql, reference.Alias, column, parameter))
            {
                return new MissingReference($"{reference.Alias}.{column}");
            }
        }

        return null;
    }

    private static MissingReference? FirstMissingSoftDeleteReference(
        string sql,
        IReadOnlyList<TableReference> references,
        IReadOnlySet<string> softDeleteTables)
    {
        foreach (var reference in references)
        {
            if (!softDeleteTables.Contains(reference.Table))
            {
                continue;
            }

            if (!reference.HasExplicitAlias)
            {
                if (!DeletedAtFilterPattern.IsMatch(sql))
                {
                    return new MissingReference("deleted_at predicate");
                }

                continue;
            }

            if (!HasQualifiedSoftDeletePredicate(sql, reference.Alias))
            {
                return new MissingReference($"{reference.Alias}.deleted_at");
            }
        }

        return null;
    }

    private static IReadOnlyList<TableReference> ExtractTableReferences(string sql)
    {
        var references = new List<TableReference>();
        foreach (Match match in TableReferencePattern.Matches(sql))
        {
            var table = match.Groups["table"].Value;
            if (string.IsNullOrWhiteSpace(table))
            {
                continue;
            }

            var alias = match.Groups["alias"].Success ? match.Groups["alias"].Value : string.Empty;
            if (IsReservedAlias(alias))
            {
                alias = string.Empty;
            }

            references.Add(new TableReference(table, string.IsNullOrWhiteSpace(alias) ? table : alias, !string.IsNullOrWhiteSpace(alias)));
        }

        return references;
    }

    private static bool HasQualifiedPredicate(string sql, string alias, string column, string parameter)
    {
        return Regex.IsMatch(
            sql,
            $@"\b{Regex.Escape(alias)}\.{Regex.Escape(column)}\s*(?:=|IN)\s*{Regex.Escape(parameter)}\b",
            RegexOptions.IgnoreCase);
    }

    private static bool HasQualifiedSoftDeletePredicate(string sql, string alias)
    {
        return Regex.IsMatch(
            sql,
            $@"\b{Regex.Escape(alias)}\.deleted_at\s+IS\s+NULL\b",
            RegexOptions.IgnoreCase);
    }

    private static bool IsReservedAlias(string alias)
    {
        return alias.Equals("WHERE", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("JOIN", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("SET", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("VALUES", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("LIMIT", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("ORDER", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals("GROUP", StringComparison.OrdinalIgnoreCase);
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
        if (statementType is "WITH")
        {
            statementType = "SELECT";
        }

        return statementType is "SELECT" or "UPDATE" or "DELETE" or "INSERT";
    }

    private static string TrimSql(string sql)
    {
        return sql.Replace('\n', ' ').Trim();
    }

    private sealed record TableReference(string Table, string Alias, bool HasExplicitAlias);

    private sealed record MissingReference(string ExpectedPredicate);
}
