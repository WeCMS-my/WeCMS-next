using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public sealed class NoopAuditWriter : IAuditWriter
{
    public ValueTask WriteAsync(AuditWriteRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        return ValueTask.CompletedTask;
    }
}
