using WeCms.EventBus;

namespace WeCms.Modules.Configuration.Events;

public sealed record SettingChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    string SettingKey)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "configuration.setting.changed";
}

public sealed record DictChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    string Resource,
    long TargetId)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "configuration.dict.changed";
}

public sealed record I18nChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    string? TraceId,
    string? TenantId,
    long MessageId,
    string Locale,
    string MessageKey)
    : IntegrationEventBase(Id, EventType, OccurredAt, TraceId, TenantId)
{
    public const string EventType = "configuration.i18n.changed";
}
