namespace WeCms.EventBus;

public interface IIntegrationEvent
{
    Guid Id { get; }

    string Type { get; }

    DateTimeOffset OccurredAt { get; }

    string? TraceId { get; }

    string? TenantId { get; }
}
