using SqlSugar;

namespace WeCms.EventBus.SqlSugar.Entities;

[SugarTable("sys_outbox_message")]
[SugarIndex("ux_sys_outbox_message_event_id", nameof(EventId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_outbox_message_status_available", nameof(Status), OrderByType.Asc, nameof(AvailableAt), OrderByType.Asc, nameof(LockedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_outbox_message_event_type", nameof(EventType), OrderByType.Asc)]
public sealed class OutboxMessageEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "event_id", Length = 36)]
    public string EventId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "event_type", Length = 160)]
    public string EventType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "aggregate_type", Length = 120, IsNullable = true)]
    public string? AggregateType { get; set; }

    [SugarColumn(ColumnName = "aggregate_id", Length = 128, IsNullable = true)]
    public string? AggregateId { get; set; }

    [SugarColumn(ColumnName = "payload_json", ColumnDataType = "json")]
    public string PayloadJson { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "status", Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "retry_count")]
    public int RetryCount { get; set; }

    [SugarColumn(ColumnName = "available_at")]
    public DateTime AvailableAt { get; set; }

    [SugarColumn(ColumnName = "locked_at", IsNullable = true)]
    public DateTime? LockedAt { get; set; }

    [SugarColumn(ColumnName = "lock_token", Length = 36, IsNullable = true)]
    public string? LockToken { get; set; }

    [SugarColumn(ColumnName = "processed_at", IsNullable = true)]
    public DateTime? ProcessedAt { get; set; }

    [SugarColumn(ColumnName = "error", ColumnDataType = "text", IsNullable = true)]
    public string? Error { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
