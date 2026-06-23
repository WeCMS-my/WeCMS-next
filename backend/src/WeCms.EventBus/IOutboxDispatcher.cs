namespace WeCms.EventBus;

public interface IOutboxDispatcher
{
    Task<OutboxDispatchResult> DispatchAsync(CancellationToken cancellationToken);
}

public sealed record OutboxDispatchResult(int LockedCount, int ProcessedCount, int FailedCount)
{
    public static OutboxDispatchResult Empty { get; } = new(0, 0, 0);
}
