namespace WeCms.EventBus;

public interface IEventHandlerExecutor
{
    Task ExecuteAsync(
        IIntegrationEvent integrationEvent,
        IEventHandlerIdempotencyStore idempotencyStore,
        CancellationToken cancellationToken);
}
