namespace WeCms.EventBus;

public sealed record OutboxMessageWriteRecord(
    Guid EventId,
    string EventType,
    string? AggregateType,
    string? AggregateId,
    string PayloadJson,
    DateTimeOffset AvailableAt,
    DateTimeOffset CreatedAt);

public sealed record OutboxMessageRecord(
    long Id,
    Guid EventId,
    string EventType,
    string? AggregateType,
    string? AggregateId,
    string PayloadJson,
    string Status,
    int RetryCount,
    DateTimeOffset AvailableAt,
    DateTimeOffset? LockedAt,
    string? LockToken,
    DateTimeOffset? ProcessedAt,
    string? Error,
    DateTimeOffset CreatedAt);
