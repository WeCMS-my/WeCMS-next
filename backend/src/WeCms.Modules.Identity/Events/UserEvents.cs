using WeCms.EventBus;

namespace WeCms.Modules.Identity.Events;

public sealed record UserCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    long UserId)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "identity.user.created";
}

public sealed record UserDisabledEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    long UserId)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "identity.user.disabled";
}
