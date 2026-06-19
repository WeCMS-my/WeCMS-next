using WeCms.Persistence.Migration;

namespace WeCms.Tests.Unit.Persistence;

public sealed class DbMigrationRunnerTests
{
    [Fact]
    public void SplitSqlStatements_SplitsMultipleStatementsIgnoringWhitespaceAndLineComments()
    {
        var sql = """
        CREATE TABLE t_a (id INT);
        -- comment with ; in it;
        CREATE TABLE t_b (id INT)
        """;

        var statements = DbMigrationRunner.SplitSqlStatements(sql);

        Assert.Equal(2, statements.Count);
        Assert.Equal("CREATE TABLE t_a (id INT)", statements[0]);
        Assert.Equal("CREATE TABLE t_b (id INT)", statements[1]);
    }

    [Fact]
    public void SplitSqlStatements_IgnoresSemicolonInsideQuotedValues()
    {
        var sql = "INSERT INTO t VALUES ('a;b');\nUPDATE t SET c = 'z;y';";

        var statements = DbMigrationRunner.SplitSqlStatements(sql);

        Assert.Equal(2, statements.Count);
        Assert.Equal("INSERT INTO t VALUES ('a;b')", statements[0]);
        Assert.Equal("UPDATE t SET c = 'z;y'", statements[1]);
    }

    [Fact]
    public void SplitSqlStatements_ParsesProcedureBodyAsSingleStatement()
    {
        var sql = """
        CREATE PROCEDURE migrate_demo()
        BEGIN
            INSERT INTO t VALUES (1);
            INSERT INTO t VALUES (2);
        END;
        """;

        var statements = DbMigrationRunner.SplitSqlStatements(sql);

        Assert.Single(statements);
        Assert.Contains("INSERT INTO t VALUES (1);", statements[0]);
        Assert.Contains("INSERT INTO t VALUES (2);", statements[0]);
    }

    [Fact]
    public void SplitSqlStatements_RespectsProcedureCommentContent()
    {
        var sql = """
        CREATE PROCEDURE p()
        BEGIN
            -- comment; with semicolon
            INSERT INTO t VALUES ('v;1');
        END;
        """;

        var statements = DbMigrationRunner.SplitSqlStatements(sql);

        Assert.Single(statements);
    }
}
