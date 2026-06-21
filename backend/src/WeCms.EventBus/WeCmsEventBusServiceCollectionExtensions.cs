using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeCms.EventBus;

public static class WeCmsEventBusServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configure = null,
        Action<OutboxDispatcherOptions>? configureDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EventBusOptions();
        configure?.Invoke(options);
        var dispatcherOptions = new OutboxDispatcherOptions();
        configureDispatcher?.Invoke(dispatcherOptions);

        services.TryAddSingleton(options);
        services.TryAddSingleton(dispatcherOptions);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IEventBus, InMemoryEventBus>();
        services.TryAddScoped<IEventHandlerExecutor, EventHandlerExecutor>();
        services.TryAddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.TryAddSingleton<IEventHandlerIdempotencyStore, InMemoryEventHandlerIdempotencyStore>();
        services.TryAddSingleton<IIntegrationEventSerializer, SystemTextJsonIntegrationEventSerializer>();
        services.TryAddSingleton<IOutboxLockTokenProvider, GuidOutboxLockTokenProvider>();

        return services;
    }

    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEventHandler<TEvent>, THandler>();

        return services;
    }

    public static IServiceCollection AddIntegrationEvent<TEvent>(
        this IServiceCollection services,
        string eventType)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new IntegrationEventRegistration(eventType, typeof(TEvent)));

        return services;
    }
}
