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
        var sql = "SELECT id FROM sys_role WHERE deleted_at IS NULL AND code = @code";

        var exception = Record.Exception(() => RawSqlFilterGuard.RequireDeletedAtFilter(sql, "ListAsync", SoftDeleteTables));

        Assert.Null(exception);
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
    public void RequireDeletedAtFilter_ThrowsWhenSqlEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => RawSqlFilterGuard.RequireDeletedAtFilter("   ", "ListAsync"));

        Assert.Equal("sql", exception.ParamName);
    }
}
