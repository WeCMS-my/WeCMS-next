using Microsoft.Extensions.Logging;

namespace WeCms.EventBus;

public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private const int MaximumBatchSize = 100;
    private readonly IOutboxMessageRepository repository;
    private readonly IIntegrationEventSerializer serializer;
    private readonly IEventHandlerExecutor handlerExecutor;
    private readonly IEventHandlerIdempotencyStore idempotencyStore;
    private readonly IOutboxLockTokenProvider lockTokenProvider;
    private readonly TimeProvider timeProvider;
    private readonly OutboxDispatcherOptions options;
    private readonly ILogger<OutboxDispatcher> logger;

    public OutboxDispatcher(
        IOutboxMessageRepository repository,
        IIntegrationEventSerializer serializer,
        IEventHandlerExecutor handlerExecutor,
        IEventHandlerIdempotencyStore idempotencyStore,
        IOutboxLockTokenProvider lockTokenProvider,
        TimeProvider timeProvider,
        OutboxDispatcherOptions options,
        ILogger<OutboxDispatcher> logger)
    {
        this.repository = repository;
        this.serializer = serializer;
        this.handlerExecutor = handlerExecutor;
        this.idempotencyStore = idempotencyStore;
        this.lockTokenProvider = lockTokenProvider;
        this.timeProvider = timeProvider;
        this.options = options;
        this.logger = logger;
    }

    public async Task DispatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lockToken = lockTokenProvider.CreateLockToken();
        var messages = await repository
            .LockPendingMessagesAsync(ValidatedBatchSize(), now, lockToken, cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchMessageAsync(message, now, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchMessageAsync(
        OutboxMessageRecord message,
        DateTimeOffset dispatchStartedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = serializer.Deserialize(message.EventType, message.PayloadJson);
            if (integrationEvent.Id != message.EventId)
            {
                throw new InvalidOperationException($"Outbox payload event id '{integrationEvent.Id}' does not match row event id '{message.EventId}'.");
            }

            await handlerExecutor.ExecuteAsync(integrationEvent, idempotencyStore, cancellationToken).ConfigureAwait(false);
            await repository.MarkProcessedAsync(message.Id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Outbox dispatch failed for event {EventId} type {EventType}.",
                message.EventId,
                message.EventType);

            await repository.MarkFailedAsync(
                message.Id,
                TrimError(exception.Message),
                dispatchStartedAt.Add(options.RetryDelay),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private int ValidatedBatchSize()
    {
        if (options.BatchSize < 1 || options.BatchSize > MaximumBatchSize)
        {
            throw new InvalidOperationException($"Outbox dispatcher batch size must be between 1 and {MaximumBatchSize}.");
        }

        return options.BatchSize;
    }

    private static string TrimError(string error)
    {
        const int maximumLength = 1_000;
        return error.Length <= maximumLength ? error : error[..maximumLength];
    }
}
