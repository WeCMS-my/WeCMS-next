using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlAuditTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SqlAudit_RecordsSlowSql()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink, new SqlAuditOptions { SlowSqlThresholdMilliseconds = 10 });

        recorder.RecordExecuted(
            "main",
            "SELECT id FROM sys_user WHERE id = @id",
            [new SugarParameter("@id", 1)],
            TimeSpan.FromMilliseconds(15),
            affectedRows: 1);

        var record = Assert.Single(sink.Records);
        Assert.True(record.IsSlowSql);
        Assert.Equal(15, record.ElapsedMs);
        Assert.Equal("SELECT", record.OperationType);
    }

    [Fact]
    public void SqlAudit_RecordsFailedSql()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink);

        recorder.RecordFailed(
            "main",
            "UPDATE sys_user SET display_name = @displayName WHERE id = @id",
            [new SugarParameter("@displayName", "Admin"), new SugarParameter("@id", 1)],
            new InvalidOperationException("sql failed"));

        var record = Assert.Single(sink.Records);
        Assert.False(record.IsSlowSql);
        Assert.Equal("UPDATE", record.OperationType);
        Assert.Equal("sql failed", record.ErrorMessage);
    }

    [Fact]
    public void SqlAudit_RedactsSensitiveParameters()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink, new SqlAuditOptions { CaptureAllSql = true });

        recorder.RecordExecuted(
            "main",
            "INSERT INTO sys_user (username, password_hash) VALUES (@username, @password)",
            [new SugarParameter("@username", "admin"), new SugarParameter("@password", "plain-secret")],
            TimeSpan.FromMilliseconds(1));

        var record = Assert.Single(sink.Records);
        Assert.Equal("admin", record.ParametersRedacted["@username"]);
        Assert.Equal(SqlAuditRedactor.RedactedValue, record.ParametersRedacted["@password"]);
        Assert.DoesNotContain("plain-secret", string.Join(";", record.ParametersRedacted.Values), StringComparison.Ordinal);
    }

    [Fact]
    public void SqlAudit_RedactsKnownSensitiveFieldNames()
    {
        var parameters = new[]
        {
            new SugarParameter("@password_hash", "a"),
            new SugarParameter("@token", "b"),
            new SugarParameter("@refresh_token", "c"),
            new SugarParameter("@access_token", "d"),
            new SugarParameter("@secret", "e"),
            new SugarParameter("@two_factor", "f"),
            new SugarParameter("@recovery_code", "g"),
            new SugarParameter("@private_key", "h"),
            new SugarParameter("@connection_string", "i")
        };

        var redacted = new SqlAuditRedactor().Redact(parameters);

        Assert.All(redacted, pair => Assert.Equal(SqlAuditRedactor.RedactedValue, pair.Value));
    }

    [Fact]
    public void SqlAudit_IncludesRequiredRecordFields()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink, new SqlAuditOptions { CaptureAllSql = true });

        recorder.RecordExecuted(
            "audit",
            "DELETE FROM sys_login_log WHERE id = @id",
            [new SugarParameter("@id", 7)],
            TimeSpan.FromMilliseconds(2),
            affectedRows: 1);

        var record = Assert.Single(sink.Records);
        Assert.Equal("trace-1", record.TraceId);
        Assert.Equal(100, record.UserId);
        Assert.Equal("admin", record.Username);
        Assert.Equal(200, record.TenantId);
        Assert.Equal("audit", record.ConnectionName);
        Assert.Equal("UserRepository", record.RepositoryName);
        Assert.Equal("DELETE", record.OperationType);
        Assert.False(string.IsNullOrWhiteSpace(record.SqlHash));
        Assert.Equal("DELETE FROM sys_login_log WHERE id = @id", record.SqlTemplate);
        Assert.Equal("7", record.ParametersRedacted["@id"]);
        Assert.Equal(2, record.ElapsedMs);
        Assert.Equal(1, record.AffectedRows);
        Assert.False(record.IsSlowSql);
        Assert.Null(record.ErrorMessage);
        Assert.Equal(FixedNow, record.CreatedAt);
    }

    [Fact]
    public void SqlAudit_ProductionDefaultRecordsOnlySlowAndFailedSql()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink, new SqlAuditOptions { SlowSqlThresholdMilliseconds = 100 });

        recorder.RecordExecuted("main", "SELECT id FROM sys_user", [], TimeSpan.FromMilliseconds(10));
        recorder.RecordExecuted("main", "SELECT id FROM sys_role", [], TimeSpan.FromMilliseconds(150));
        recorder.RecordFailed("main", "SELECT id FROM missing_table", [], new InvalidOperationException("missing"));

        Assert.Equal(2, sink.Records.Count);
        Assert.Contains(sink.Records, record => record.IsSlowSql);
        Assert.Contains(sink.Records, record => record.ErrorMessage == "missing");
    }

    [Fact]
    public void SqlAudit_DoesNotAuditItselfRecursively()
    {
        var sink = new RecordingSqlAuditSink();
        var recorder = Recorder(sink, new SqlAuditOptions { CaptureAllSql = true });
        sink.OnWrite = () => recorder.RecordExecuted("audit", "INSERT INTO sys_sql_audit VALUES (@id)", [new SugarParameter("@id", 1)], TimeSpan.FromMilliseconds(1));

        recorder.RecordExecuted("main", "SELECT id FROM sys_user", [], TimeSpan.FromMilliseconds(1));

        Assert.Single(sink.Records);
    }

    private static SqlAuditRecorder Recorder(
        RecordingSqlAuditSink sink,
        SqlAuditOptions? options = null)
    {
        return new SqlAuditRecorder(
            options ?? new SqlAuditOptions(),
            sink,
            new FixedSqlAuditContextAccessor(new SqlAuditContext("trace-1", 100, "admin", 200, "UserRepository")),
            () => FixedNow);
    }

    private sealed class RecordingSqlAuditSink : ISqlAuditSink
    {
        public List<SqlAuditRecord> Records { get; } = [];

        public Action? OnWrite { get; set; }

        public void Write(SqlAuditRecord record)
        {
            Records.Add(record);
            OnWrite?.Invoke();
        }
    }

    private sealed class FixedSqlAuditContextAccessor(SqlAuditContext current) : ISqlAuditContextAccessor
    {
        public SqlAuditContext Current { get; } = current;
    }
}
