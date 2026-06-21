using System.Reflection;

namespace WeCms.EventBus;

public sealed class InMemoryEventBus(IServiceProvider serviceProvider, EventBusOptions options) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        await PublishToHandlersAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var method = typeof(InMemoryEventBus)
            .GetMethod(nameof(PublishRuntimeEventAsync), BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(integrationEvent.GetType())
            ?? throw new InvalidOperationException("Runtime event publish method was not found.");

        if (method.Invoke(this, [integrationEvent, cancellationToken]) is not Task task)
        {
            throw new InvalidOperationException("Runtime event publish method returned an invalid result.");
        }

        await task.ConfigureAwait(false);
    }

    private Task PublishRuntimeEventAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        return PublishToHandlersAsync(integrationEvent, cancellationToken);
    }

    private async Task PublishToHandlersAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = ResolveHandlers<TEvent>();
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler.HandleAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
            catch when (options.HandlerFailureBehavior == EventBusHandlerFailureBehavior.Continue
                && !cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private IEnumerable<IEventHandler<TEvent>> ResolveHandlers<TEvent>()
        where TEvent : IIntegrationEvent
    {
        return serviceProvider.GetService(typeof(IEnumerable<IEventHandler<TEvent>>)) as IEnumerable<IEventHandler<TEvent>>
            ?? [];
    }
}
