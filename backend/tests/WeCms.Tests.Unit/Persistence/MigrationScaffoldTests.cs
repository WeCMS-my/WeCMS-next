using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.Persistence;

public sealed class MigrationScaffoldTests
{
    [Fact]
    public void MigrationScaffold_ProducesReviewableBaselineDiff()
    {
        var result = new SqlSugarSchemaValidationResult(
            [new SqlSugarMissingTable(typeof(MigrationScaffoldTests), "sys_missing")],
            [new SqlSugarMissingColumn("sys_user", "display_name")],
            [new SqlSugarNullableMismatch("sys_user", "email", true, false)],
            [new SqlSugarLengthMismatch("sys_user", "username", 64, 32)],
            [new SqlSugarIndexMismatch("sys_user", "ux_sys_user_username", "missing index")]);

        var output = new MigrationScaffold().CreateReviewableDiff(result);

        Assert.Contains("-- WeCMS schema validation diff", output, StringComparison.Ordinal);
        Assert.Contains("MISSING_TABLE sys_missing", output, StringComparison.Ordinal);
        Assert.Contains("MISSING_COLUMN sys_user.display_name", output, StringComparison.Ordinal);
        Assert.Contains("NULLABLE_MISMATCH sys_user.email expected=True actual=False", output, StringComparison.Ordinal);
        Assert.Contains("LENGTH_MISMATCH sys_user.username expected=64 actual=32", output, StringComparison.Ordinal);
        Assert.Contains("INDEX_MISMATCH sys_user.ux_sys_user_username missing index", output, StringComparison.Ordinal);
    }
}
