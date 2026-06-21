using SqlSugar;

namespace WeCms.Modules.Audit.SqlSugar.Entities;

[SugarTable("sys_audit_log")]
[SugarIndex("ix_sys_audit_log_user_id", nameof(UserId), OrderByType.Asc)]
[SugarIndex("ix_sys_audit_log_created_at", nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_audit_log_module_resource_action", nameof(Module), OrderByType.Asc)]
public sealed class AuditLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = true)]
    public long? UserId { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Username { get; set; }

    [SugarColumn(Length = 80)]
    public string Module { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Resource { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Action { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "target_id", Length = 128, IsNullable = true)]
    public string? TargetId { get; set; }

    [SugarColumn(ColumnName = "request_method", Length = 16)]
    public string RequestMethod { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "request_path", Length = 160)]
    public string RequestPath { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "ip_address", Length = 64, IsNullable = true)]
    public string? IpAddress { get; set; }

    [SugarColumn(ColumnName = "user_agent", Length = 500, IsNullable = true)]
    public string? UserAgent { get; set; }

    [SugarColumn(ColumnName = "trace_id", Length = 64, IsNullable = true)]
    public string? TraceId { get; set; }

    [SugarColumn(Length = 32)]
    public string Result { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string Detail { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
