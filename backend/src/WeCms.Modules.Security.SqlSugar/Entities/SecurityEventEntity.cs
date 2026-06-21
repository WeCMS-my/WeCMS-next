using SqlSugar;

namespace WeCms.Modules.Security.SqlSugar.Entities;

[SugarTable("sys_security_event")]
[SugarIndex("ix_sys_security_event_type", nameof(EventType), OrderByType.Asc)]
[SugarIndex("ix_sys_security_event_user_id", nameof(UserId), OrderByType.Asc)]
[SugarIndex("ix_sys_security_event_source", nameof(Source), OrderByType.Asc)]
[SugarIndex("ix_sys_security_event_trace_id", nameof(TraceId), OrderByType.Asc)]
public sealed class SecurityEventEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "event_type", Length = 80)]
    public string EventType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "user_id", IsNullable = true)]
    public long? UserId { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Username { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Ip { get; set; }

    [SugarColumn(Length = 32)]
    public string Severity { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string Message { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "trace_id", Length = 64)]
    public string TraceId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
