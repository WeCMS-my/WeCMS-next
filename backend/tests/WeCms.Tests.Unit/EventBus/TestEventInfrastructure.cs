using WeCms.EventBus;
using WeCms.Shared.Id;

namespace WeCms.Tests.Unit;

internal sealed class NullOutboxWriter : IOutboxWriter
{
    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        return Task.CompletedTask;
    }
}

internal sealed class RecordingOutboxWriter : IOutboxWriter
{
    private readonly List<string>? _operations;

    public RecordingOutboxWriter(List<string>? operations = null)
    {
        _operations = operations;
    }

    public List<IIntegrationEvent> Events { get; } = [];

    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        Events.Add(integrationEvent);
        _operations?.Add($"outbox:{integrationEvent.Type}");
        return Task.CompletedTask;
    }
}

internal sealed class FixedTestIdGenerator : IIdGenerator
{
    public string NewId()
    {
        return "00000000000000000000000000000001";
    }
}
