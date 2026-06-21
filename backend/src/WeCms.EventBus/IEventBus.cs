namespace WeCms.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;

    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
