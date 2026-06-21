namespace WeCms.Data.SqlSugar;

public enum QueryFilterBypassTarget
{
    All = 0
}

public sealed record QueryFilterBypassAuditEvent(
    QueryFilterBypassTarget Target,
    string Reason,
    DateTime CreatedAtUtc);

public interface IQueryFilterBypassAuditSink
{
    void Write(QueryFilterBypassAuditEvent auditEvent);
}

public sealed class NullQueryFilterBypassAuditSink : IQueryFilterBypassAuditSink
{
    public void Write(QueryFilterBypassAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
    }
}

public sealed class QueryFilterBypass
{
    private static readonly AsyncLocal<QueryFilterBypassTarget?> CurrentTarget = new();
    private readonly IQueryFilterBypassAuditSink _auditSink;

    public QueryFilterBypass(IQueryFilterBypassAuditSink auditSink)
    {
        _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    }

    public bool IsBypassed => CurrentTarget.Value is not null;

    public IDisposable BypassAll(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("QueryFilter bypass reason is required.", nameof(reason));
        }

        var previous = CurrentTarget.Value;
        CurrentTarget.Value = QueryFilterBypassTarget.All;
        _auditSink.Write(new QueryFilterBypassAuditEvent(QueryFilterBypassTarget.All, reason, DateTime.UtcNow));

        return new Scope(() => CurrentTarget.Value = previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Scope(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _dispose();
            _disposed = true;
        }
    }
}
