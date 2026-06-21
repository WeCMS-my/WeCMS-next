using WeCms.EventBus;

namespace WeCms.Modules.AccessControl.Events;

public sealed record RolePermissionsChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    long RoleId,
    IReadOnlyList<long> PermissionIds)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "access_control.role_permissions.changed";
}

public sealed record MenuChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    IReadOnlyList<long> MenuIds)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "access_control.menu.changed";
}
