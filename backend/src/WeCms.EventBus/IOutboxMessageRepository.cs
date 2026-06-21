namespace WeCms.EventBus;

public interface IOutboxMessageRepository
{
    Task WriteAsync(OutboxMessageWriteRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessageRecord>> LockPendingMessagesAsync(
        int batchSize,
        DateTimeOffset now,
        string lockToken,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(long id, DateTimeOffset processedAt, CancellationToken cancellationToken);

    Task MarkFailedAsync(long id, string error, DateTimeOffset nextAvailableAt, CancellationToken cancellationToken);
}
