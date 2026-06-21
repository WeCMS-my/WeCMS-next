namespace WeCms.EventBus;

public interface IEventHandlerIdempotencyStore
{
    Task<EventHandlingClaimResult> TryStartAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken);
}
