using WeCms.EventBus;

namespace WeCms.Modules.Security.Events;

public sealed record SecurityBanCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    long BanId,
    string BanType,
    string Target,
    string Severity)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "security.ban.created";
}
