using System.Reflection;
using System.Runtime.ExceptionServices;

namespace WeCms.EventBus;

public sealed class EventHandlerExecutor(IServiceProvider serviceProvider, EventBusOptions options) : IEventHandlerExecutor
{
    public async Task ExecuteAsync(
        IIntegrationEvent integrationEvent,
        IEventHandlerIdempotencyStore idempotencyStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(idempotencyStore);

        var handlers = ResolveHandlers(integrationEvent.GetType());
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handlerKey = HandlerKey(handler);
            var claim = await idempotencyStore.TryStartAsync(integrationEvent.Id, handlerKey, cancellationToken).ConfigureAwait(false);
            if (claim == EventHandlingClaimResult.AlreadyProcessed)
            {
                continue;
            }

            if (claim == EventHandlingClaimResult.AlreadyProcessing)
            {
                throw new InvalidOperationException($"Integration event handler '{handlerKey}' is already processing event '{integrationEvent.Id}'.");
            }

            try
            {
                await InvokeHandlerAsync(handler, integrationEvent, cancellationToken).ConfigureAwait(false);
                await idempotencyStore.MarkProcessedAsync(integrationEvent.Id, handlerKey, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await idempotencyStore.MarkFailedAsync(integrationEvent.Id, handlerKey, cancellationToken).ConfigureAwait(false);

                if (options.HandlerFailureBehavior == EventBusHandlerFailureBehavior.Rethrow)
                {
                    throw;
                }
            }
        }
    }

    private object[] ResolveHandlers(Type eventClrType)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventClrType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
        var handlers = serviceProvider.GetService(enumerableType) as System.Collections.IEnumerable;

        return handlers?.Cast<object>().ToArray() ?? [];
    }

    private static async Task InvokeHandlerAsync(
        object handler,
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var method = handler
            .GetType()
            .GetMethod(nameof(IEventHandler<IIntegrationEvent>.HandleAsync), BindingFlags.Instance | BindingFlags.Public);

        object? result;
        try
        {
            result = method?.Invoke(handler, [integrationEvent, cancellationToken]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        if (result is not Task task)
        {
            throw new InvalidOperationException($"Integration event handler '{HandlerKey(handler)}' has an invalid HandleAsync method.");
        }

        await task.ConfigureAwait(false);
    }

    private static string HandlerKey(object handler)
    {
        return handler.GetType().FullName ?? handler.GetType().Name;
    }
}
