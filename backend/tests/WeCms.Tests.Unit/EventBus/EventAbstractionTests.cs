using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WeCms.EventBus;

namespace WeCms.Tests.Unit.EventBus;

public sealed class EventAbstractionTests
{
    [Fact]
    public void IntegrationEventBase_ExposesRequiredMetadata()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var occurredAt = new DateTimeOffset(2026, 6, 21, 1, 2, 3, TimeSpan.Zero);

        var integrationEvent = new TestIntegrationEvent(
            id,
            "test.event",
            occurredAt,
            "trace-001",
            "tenant-001");

        Assert.Equal(id, integrationEvent.Id);
        Assert.Equal("test.event", integrationEvent.Type);
        Assert.Equal(occurredAt, integrationEvent.OccurredAt);
        Assert.Equal("trace-001", integrationEvent.TraceId);
        Assert.Equal("tenant-001", integrationEvent.TenantId);
    }

    [Fact]
    public void IntegrationEventBase_RejectsEmptyIdAndType()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 21, 1, 2, 3, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new TestIntegrationEvent(
            Guid.Empty,
            "test.event",
            occurredAt,
            "trace-001",
            "tenant-001"));
        Assert.Throws<ArgumentException>(() => new TestIntegrationEvent(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "",
            occurredAt,
            "trace-001",
            "tenant-001"));
    }

    [Fact]
    public void EventHandler_RequiresCancellationToken()
    {
        var method = typeof(IEventHandler<TestIntegrationEvent>).GetMethod(nameof(IEventHandler<TestIntegrationEvent>.HandleAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Collection(
            parameters,
            parameter => Assert.Equal(typeof(TestIntegrationEvent), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CancellationToken), parameter.ParameterType));
    }

    [Fact]
    public void PublishAndOutboxAbstractions_RequireCancellationToken()
    {
        AssertMethodRequiresCancellationToken(typeof(IEventBus), "PublishAsync");
        AssertMethodRequiresCancellationToken(typeof(IOutboxWriter), "WriteAsync");
        AssertMethodRequiresCancellationToken(typeof(IOutboxDispatcher), "DispatchAsync");
    }

    private static void AssertMethodRequiresCancellationToken(Type type, string methodName)
    {
        var methods = type.GetMethods().Where(method => method.Name == methodName).ToArray();
        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
        {
            Assert.Equal(typeof(Task), method.ReturnType);
            Assert.Contains(method.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        });
    }

    private sealed record TestIntegrationEvent(
        Guid Id,
        string Type,
        DateTimeOffset OccurredAt,
        string? TraceId,
        string? TenantId) : IntegrationEventBase(Id, Type, OccurredAt, TraceId, TenantId);
}

public sealed class InMemoryEventBusTests
{
    [Fact]
    public async Task EventBus_PublishesToHandlers()
    {
        var services = new ServiceCollection();
        var recorder = new RecordingHandlerState();
        services.AddSingleton(recorder);
        services.AddWeCmsEventBus();
        services.AddEventHandler<TestIntegrationEvent, RecordingHandler>();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();
        using var cancellationTokenSource = new CancellationTokenSource();

        var integrationEvent = TestIntegrationEvent.Create();
        await eventBus.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        Assert.Same(integrationEvent, recorder.HandledEvent);
        Assert.Equal(cancellationTokenSource.Token, recorder.CancellationToken);
    }

    [Fact]
    public async Task EventBus_HandlerFailureDoesNotSwallowException_WhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddWeCmsEventBus(options => options.HandlerFailureBehavior = EventBusHandlerFailureBehavior.Rethrow);
        services.AddEventHandler<TestIntegrationEvent, FailingHandler>();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => eventBus.PublishAsync(
            TestIntegrationEvent.Create(),
            CancellationToken.None));
    }

    [Fact]
    public async Task EventBus_ContinuesOnHandlerFailure_WhenExplicitlyConfigured()
    {
        var services = new ServiceCollection();
        var recorder = new RecordingHandlerState();
        services.AddSingleton(recorder);
        services.AddWeCmsEventBus(options => options.HandlerFailureBehavior = EventBusHandlerFailureBehavior.Continue);
        services.AddEventHandler<TestIntegrationEvent, FailingHandler>();
        services.AddEventHandler<TestIntegrationEvent, RecordingHandler>();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        await eventBus.PublishAsync(TestIntegrationEvent.Create(), CancellationToken.None);

        Assert.NotNull(recorder.HandledEvent);
    }

    [Fact]
    public void AddEventHandler_RegistersHandlersExplicitlyThroughDi()
    {
        var services = new ServiceCollection();
        services.AddWeCmsEventBus();
        services.AddEventHandler<TestIntegrationEvent, RecordingHandler>();
        services.AddSingleton(new RecordingHandlerState());
        var provider = services.BuildServiceProvider();

        var handlers = provider.GetRequiredService<IEnumerable<IEventHandler<TestIntegrationEvent>>>();

        Assert.Single(handlers);
        Assert.IsType<RecordingHandler>(handlers.Single());
    }

    private sealed record TestIntegrationEvent(
        Guid Id,
        string Type,
        DateTimeOffset OccurredAt,
        string? TraceId,
        string? TenantId) : IntegrationEventBase(Id, Type, OccurredAt, TraceId, TenantId)
    {
        public static TestIntegrationEvent Create()
        {
            return new TestIntegrationEvent(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "test.event",
                new DateTimeOffset(2026, 6, 21, 1, 2, 3, TimeSpan.Zero),
                "trace-002",
                "tenant-002");
        }
    }

    private sealed class RecordingHandlerState
    {
        public TestIntegrationEvent? HandledEvent { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }

    private sealed class RecordingHandler(RecordingHandlerState state) : IEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            state.HandledEvent = integrationEvent;
            state.CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed.");
        }
    }
}

public sealed class OutboxDispatcherTests
{
    [Fact]
    public async Task OutboxDispatcher_DispatchesPendingMessages()
    {
        var now = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);
        var repository = new FakeOutboxMessageRepository
        {
            LockedMessages = [CreateMessage(101, Guid.Parse("11111111-1111-1111-1111-111111111111"), now)]
        };
        var executor = new FakeEventHandlerExecutor();
        var dispatcher = CreateDispatcher(repository, executor, now);

        await dispatcher.DispatchAsync(CancellationToken.None);

        Assert.Single(executor.ExecutedEvents);
        Assert.Equal(101, repository.ProcessedIds.Single());
        Assert.Empty(repository.FailedMessages);
        Assert.Equal(10, repository.LockBatchSizes.Single());
    }

    [Fact]
    public async Task OutboxDispatcher_RetriesFailedMessages()
    {
        var now = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);
        var eventId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var repository = new FakeOutboxMessageRepository
        {
            LockedMessages = [CreateMessage(102, eventId, now)]
        };
        var executor = new FakeEventHandlerExecutor { Failure = new InvalidOperationException("handler failed") };
        var dispatcher = CreateDispatcher(repository, executor, now);

        await dispatcher.DispatchAsync(CancellationToken.None);

        Assert.Empty(executor.ExecutedEvents);
        var failure = repository.FailedMessages.Single();
        Assert.Equal(102, failure.Id);
        Assert.Contains("handler failed", failure.Error, StringComparison.Ordinal);
        Assert.Equal(now.AddMinutes(5), failure.NextAvailableAt);
    }

    [Fact]
    public async Task OutboxDispatcher_DoesNotDoubleProcessLockedMessage()
    {
        var now = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);
        var eventId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var repository = new FakeOutboxMessageRepository
        {
            LockedMessages = [CreateMessage(103, eventId, now)]
        };
        var idempotencyStore = new InMemoryEventHandlerIdempotencyStore();
        Assert.Equal(
            EventHandlingClaimResult.Started,
            await idempotencyStore.TryStartAsync(eventId, FakeEventHandlerExecutor.HandlerKey, CancellationToken.None));
        var executor = new FakeEventHandlerExecutor();
        var dispatcher = CreateDispatcher(repository, executor, now, idempotencyStore);

        await dispatcher.DispatchAsync(CancellationToken.None);

        Assert.Empty(executor.ExecutedEvents);
        Assert.Empty(repository.ProcessedIds);
        var failure = repository.FailedMessages.Single();
        Assert.Equal(103, failure.Id);
        Assert.Equal(now.AddMinutes(5), failure.NextAvailableAt);
    }

    [Fact]
    public async Task EventHandlers_AreIdempotent()
    {
        var services = new ServiceCollection();
        var state = new HandlerIdempotencyState();
        services.AddSingleton(state);
        services.AddWeCmsEventBus();
        services.AddEventHandler<TestDispatchEvent, FirstIdempotentHandler>();
        services.AddEventHandler<TestDispatchEvent, FailingOnceSecondHandler>();
        var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IEventHandlerExecutor>();
        var store = new InMemoryEventHandlerIdempotencyStore();
        var integrationEvent = TestDispatchEvent.Create(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(integrationEvent, store, CancellationToken.None));
        await executor.ExecuteAsync(integrationEvent, store, CancellationToken.None);

        Assert.Equal(1, state.FirstHandlerCalls);
        Assert.Equal(2, state.SecondHandlerCalls);
    }

    private static OutboxDispatcher CreateDispatcher(
        FakeOutboxMessageRepository repository,
        FakeEventHandlerExecutor executor,
        DateTimeOffset now,
        IEventHandlerIdempotencyStore? idempotencyStore = null)
    {
        return new OutboxDispatcher(
            repository,
            new FakeIntegrationEventSerializer(),
            executor,
            idempotencyStore ?? new InMemoryEventHandlerIdempotencyStore(),
            new FakeOutboxLockTokenProvider(),
            new FakeTimeProvider(now),
            new OutboxDispatcherOptions { BatchSize = 10, RetryDelay = TimeSpan.FromMinutes(5) },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxDispatcher>.Instance);
    }

    private static OutboxMessageRecord CreateMessage(long id, Guid eventId, DateTimeOffset now)
    {
        return new OutboxMessageRecord(
            id,
            eventId,
            "test.dispatch",
            null,
            null,
            $$"""{"id":"{{eventId:D}}"}""",
            OutboxMessageStatus.Processing,
            0,
            now,
            now,
            FakeOutboxLockTokenProvider.LockToken,
            null,
            null,
            now);
    }

    private sealed class FakeOutboxMessageRepository : IOutboxMessageRepository
    {
        public IReadOnlyList<OutboxMessageRecord> LockedMessages { get; init; } = [];

        public List<int> LockBatchSizes { get; } = [];

        public List<long> ProcessedIds { get; } = [];

        public List<(long Id, string Error, DateTimeOffset NextAvailableAt)> FailedMessages { get; } = [];

        public Task WriteAsync(OutboxMessageWriteRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<OutboxMessageRecord>> LockPendingMessagesAsync(
            int batchSize,
            DateTimeOffset now,
            string lockToken,
            CancellationToken cancellationToken)
        {
            LockBatchSizes.Add(batchSize);
            return Task.FromResult(LockedMessages);
        }

        public Task MarkProcessedAsync(long id, DateTimeOffset processedAt, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(long id, string error, DateTimeOffset nextAvailableAt, CancellationToken cancellationToken)
        {
            FailedMessages.Add((id, error, nextAvailableAt));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeIntegrationEventSerializer : IIntegrationEventSerializer
    {
        public IIntegrationEvent Deserialize(string eventType, string payloadJson)
        {
            using var document = System.Text.Json.JsonDocument.Parse(payloadJson);
            return new TestDispatchEvent(
                document.RootElement.GetProperty("id").GetGuid(),
                eventType,
                new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero),
                "trace-dispatch",
                "tenant-dispatch");
        }
    }

    private sealed class FakeEventHandlerExecutor : IEventHandlerExecutor
    {
        public const string HandlerKey = "test.fake-handler";

        public List<IIntegrationEvent> ExecutedEvents { get; } = [];

        public Exception? Failure { get; init; }

        public async Task ExecuteAsync(
            IIntegrationEvent integrationEvent,
            IEventHandlerIdempotencyStore idempotencyStore,
            CancellationToken cancellationToken)
        {
            var claim = await idempotencyStore.TryStartAsync(integrationEvent.Id, HandlerKey, cancellationToken);
            if (claim == EventHandlingClaimResult.AlreadyProcessed)
            {
                return;
            }

            if (claim == EventHandlingClaimResult.AlreadyProcessing)
            {
                throw new InvalidOperationException("Handler is already processing.");
            }

            if (Failure is not null)
            {
                await idempotencyStore.MarkFailedAsync(integrationEvent.Id, HandlerKey, cancellationToken);
                throw Failure;
            }

            ExecutedEvents.Add(integrationEvent);
            await idempotencyStore.MarkProcessedAsync(integrationEvent.Id, HandlerKey, cancellationToken);
        }
    }

    private sealed record TestDispatchEvent(
        Guid Id,
        string Type,
        DateTimeOffset OccurredAt,
        string? TraceId,
        string? TenantId) : IntegrationEventBase(Id, Type, OccurredAt, TraceId, TenantId)
    {
        public static TestDispatchEvent Create(Guid id)
        {
            return new TestDispatchEvent(
                id,
                "test.dispatch",
                new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero),
                "trace-dispatch",
                "tenant-dispatch");
        }
    }

    private sealed class HandlerIdempotencyState
    {
        public int FirstHandlerCalls { get; set; }

        public int SecondHandlerCalls { get; set; }
    }

    private sealed class FirstIdempotentHandler(HandlerIdempotencyState state) : IEventHandler<TestDispatchEvent>
    {
        public Task HandleAsync(TestDispatchEvent integrationEvent, CancellationToken cancellationToken)
        {
            state.FirstHandlerCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingOnceSecondHandler(HandlerIdempotencyState state) : IEventHandler<TestDispatchEvent>
    {
        public Task HandleAsync(TestDispatchEvent integrationEvent, CancellationToken cancellationToken)
        {
            state.SecondHandlerCalls++;
            if (state.SecondHandlerCalls == 1)
            {
                throw new InvalidOperationException("second handler failed once");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class FakeOutboxLockTokenProvider : IOutboxLockTokenProvider
    {
        public const string LockToken = "test-lock-token";

        public string CreateLockToken()
        {
            return LockToken;
        }
    }
}
