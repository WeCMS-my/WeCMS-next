namespace WeCms.EventBus;

public abstract record IntegrationEventBase : IIntegrationEvent
{
    protected IntegrationEventBase(Guid id, string type, DateTimeOffset occurredAt, string? traceId, string? tenantId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Integration event id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Integration event type must not be empty.", nameof(type));
        }

        Id = id;
        Type = type;
        OccurredAt = occurredAt;
        TraceId = traceId;
        TenantId = tenantId;
    }

    public Guid Id { get; }

    public string Type { get; }

    public DateTimeOffset OccurredAt { get; }

    public string? TraceId { get; }

    public string? TenantId { get; }
}
