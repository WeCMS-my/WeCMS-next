using System;
using WeCms.Data.SqlSugar;
using Xunit;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class RawSqlFilterGuardTests
{
    private static readonly IReadOnlySet<string> SoftDeleteTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sys_user",
        "sys_role",
        "sys_menu",
        "sys_permission",
        "sys_department",
        "sys_position",
        "sys_dict_type",
        "sys_dict_value",
        "sys_i18n_message"
    };

    private static readonly IReadOnlySet<string> TenantTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sys_tenant_scoped_table"
    };

    private static readonly IReadOnlySet<string> DataScopeTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sys_data_scope_table"
    };

    [Fact]
    public void RequireDeletedAtFilter_AllowsQueryWithDeletedAtPredicate()
    {
        var sql = "SELECT COUNT(1) FROM sys_user WHERE deleted_at IS NULL AND id = @id";

        var exception = Record.Exception(() => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync"));

        Assert.Null(exception);
    }

    [Fact]
    public void RequireDeletedAtFilter_ThrowsWhenDeletedAtPredicateMissing()
    {
        var sql = "SELECT COUNT(1) FROM sys_user WHERE status = @status";

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync"));

        Assert.Contains("requires an explicit deleted_at predicate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDeletedAtFilter_WithSoftDeleteTableRequiresDeletedAtPredicate()
    {
        var sql = "SELECT id FROM sys_role WHERE code = @code";

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync", SoftDeleteTables));

        Assert.Contains("requires an explicit deleted_at predicate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDeletedAtFilter_WithSoftDeleteTableAndDeletedAtPredicatePasses()
    {
        var sql = "SELECT id FROM sys_role r WHERE r.deleted_at IS NULL AND r.code = @code";

        var exception = Record.Exception(() => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync", SoftDeleteTables));

        Assert.Null(exception);
    }

    [Fact]
    public void RequireDeletedAtFilter_WithSoftDeleteTableAliasRejectsUnqualifiedPredicate()
    {
        var sql = "SELECT id FROM sys_role r WHERE deleted_at IS NULL AND r.code = @code";

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync", SoftDeleteTables));

        Assert.Contains("r.deleted_at", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDeletedAtFilter_WithSoftDeleteTableInsertSkipsDeletedAtRequirement()
    {
        var sql = "INSERT INTO sys_role (code, name) VALUES (@code, @name)";

        var exception = Record.Exception(() => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "CreateAsync", SoftDeleteTables));

        Assert.Null(exception);
    }

    [Fact]
    public void RequireDeletedAtFilter_WithNonSoftDeleteTableSkipsDeletedAtRequirement()
    {
        var sql = "SELECT id FROM sys_file WHERE is_active = TRUE";

        var exception = Record.Exception(() => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync", SoftDeleteTables));

        Assert.Null(exception);
    }

    [Fact]
    public void RequireDataBoundaryFilters_AllowsTenantAndDataScopeFiltersWhenRequired()
    {
        var context = new QueryFilterContext(42, [100, 200]);
        var sql =
            """
            SELECT id
            FROM sys_tenant_scoped_table t
            WHERE deleted_at IS NULL
              AND t.tenant_id = @tenantId
              AND t.created_by_user_id IN @userIds
            """;

        var exception = Record.Exception(
            () => RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                "ListAsync",
                context,
                SoftDeleteTables,
                TenantTables,
                DataScopeTables));

        Assert.Null(exception);
    }

    [Fact]
    public void RequireDataBoundaryFilters_ThrowsWhenTenantFilterMissing()
    {
        var context = new QueryFilterContext(42, [100, 200]);
        var sql =
            """
            SELECT id
            FROM sys_tenant_scoped_table t
            WHERE deleted_at IS NULL
              AND t.created_by_user_id IN @userIds
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                "ListAsync",
                context,
                SoftDeleteTables,
                TenantTables,
                DataScopeTables));

        Assert.Contains("tenant_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDataBoundaryFilters_ThrowsWhenTenantFilterUsesWrongAlias()
    {
        var context = new QueryFilterContext(42, []);
        var sql =
            """
            SELECT id
            FROM sys_tenant_scoped_table t
            WHERE other.tenant_id = @tenantId
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                "ListAsync",
                context,
                SoftDeleteTables,
                TenantTables,
                DataScopeTables));

        Assert.Contains("t.tenant_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDataBoundaryFilters_ThrowsWhenDataScopeFilterMissing()
    {
        var context = new QueryFilterContext(42, [100, 200]);
        var sql =
            """
            SELECT id
            FROM sys_data_scope_table t
            WHERE deleted_at IS NULL
              AND t.tenant_id = @tenantId
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                "ListAsync",
                context,
                SoftDeleteTables,
                TenantTables,
                DataScopeTables));

        Assert.Contains("created_by_user_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDataBoundaryFilters_ThrowsWhenDataScopeFilterUsesWrongAlias()
    {
        var context = new QueryFilterContext(null, [100, 200]);
        var sql =
            """
            SELECT id
            FROM sys_data_scope_table s
            WHERE other.created_by_user_id IN @dataScopeUserIds
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => RawSqlFilterGuard.RequireDataBoundaryFilters(
                sql,
                "ListAsync",
                context,
                SoftDeleteTables,
                TenantTables,
                DataScopeTables));

        Assert.Contains("s.created_by_user_id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireDeletedAtFilter_ThrowsWhenSqlEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => RawSqlFilterGuard.RequireDeletedAtFilter("   ", "ListAsync"));

        Assert.Equal("sql", exception.ParamName);
    }
}
