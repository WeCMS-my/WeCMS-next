namespace WeCms.EventBus;

public interface IOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}
