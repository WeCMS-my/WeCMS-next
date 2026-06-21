namespace WeCms.Shared.Endpoints;

public interface IAuditWriter
{
    ValueTask WriteAsync(AuditWriteRecord record, CancellationToken cancellationToken);
}
