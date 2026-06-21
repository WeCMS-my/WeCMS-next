using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class QueryFilterBypassTests
{
    [Fact]
    public void BypassFilterRequiresReason()
    {
        var auditSink = new RecordingQueryFilterBypassAuditSink();
        var bypass = new QueryFilterBypass(auditSink);

        var exception = Assert.Throws<ArgumentException>(() => bypass.BypassAll(" "));

        Assert.Contains("reason", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(auditSink.Events);
    }

    [Fact]
    public void BypassFilterWritesAudit()
    {
        var auditSink = new RecordingQueryFilterBypassAuditSink();
        var bypass = new QueryFilterBypass(auditSink);

        using (bypass.BypassAll("maintenance backfill"))
        {
            Assert.True(bypass.IsBypassed);
        }

        Assert.False(bypass.IsBypassed);
        var auditEvent = Assert.Single(auditSink.Events);
        Assert.Equal("maintenance backfill", auditEvent.Reason);
        Assert.Equal(QueryFilterBypassTarget.All, auditEvent.Target);
    }

    private sealed class RecordingQueryFilterBypassAuditSink : IQueryFilterBypassAuditSink
    {
        public List<QueryFilterBypassAuditEvent> Events { get; } = [];

        public void Write(QueryFilterBypassAuditEvent auditEvent)
        {
            Events.Add(auditEvent);
        }
    }
}
