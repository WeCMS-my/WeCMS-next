using System.Collections.Concurrent;

namespace WeCms.EventBus;

public sealed class InMemoryEventHandlerIdempotencyStore : IEventHandlerIdempotencyStore
{
    private readonly ConcurrentDictionary<EventHandlerExecutionKey, EventHandlingState> states = new();

    public Task<EventHandlingClaimResult> TryStartAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = new EventHandlerExecutionKey(eventId, handlerKey);
        if (states.TryAdd(key, EventHandlingState.Processing))
        {
            return Task.FromResult(EventHandlingClaimResult.Started);
        }

        var state = states[key];
        return Task.FromResult(state switch
        {
            EventHandlingState.Processing => EventHandlingClaimResult.AlreadyProcessing,
            EventHandlingState.Processed => EventHandlingClaimResult.AlreadyProcessed,
            _ => throw new InvalidOperationException($"Unsupported event handling state {state}.")
        });
    }

    public Task MarkProcessedAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        states[new EventHandlerExecutionKey(eventId, handlerKey)] = EventHandlingState.Processed;
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid eventId, string handlerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        states.TryRemove(new EventHandlerExecutionKey(eventId, handlerKey), out _);
        return Task.CompletedTask;
    }

    private readonly record struct EventHandlerExecutionKey(Guid EventId, string HandlerKey);

    private enum EventHandlingState
    {
        Processing,
        Processed
    }
}
