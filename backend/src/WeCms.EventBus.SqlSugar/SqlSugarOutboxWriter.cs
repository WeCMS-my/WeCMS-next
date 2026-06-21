using System.Text.Json;
using WeCms.EventBus;

namespace WeCms.EventBus.SqlSugar;

public sealed class SqlSugarOutboxWriter(IOutboxMessageRepository repository) : IOutboxWriter
{
    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var record = new OutboxMessageWriteRecord(
            integrationEvent.Id,
            integrationEvent.Type,
            null,
            null,
            JsonSerializer.Serialize(integrationEvent),
            integrationEvent.OccurredAt,
            integrationEvent.OccurredAt);

        return repository.WriteAsync(record, cancellationToken);
    }
}
