using SqlSugar;

namespace WeCms.Modules.Audit.SqlSugar.Entities;

[SugarTable("sys_login_log")]
[SugarIndex("ix_sys_login_log_username", nameof(Username), OrderByType.Asc)]
[SugarIndex("ix_sys_login_log_user_id", nameof(UserId), OrderByType.Asc)]
public sealed class LoginLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(Length = 64)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "user_id", IsNullable = true)]
    public long? UserId { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Ip { get; set; }

    [SugarColumn(ColumnName = "user_agent", Length = 500, IsNullable = true)]
    public string? UserAgent { get; set; }

    [SugarColumn(Length = 32)]
    public string Result { get; set; } = string.Empty;

    [SugarColumn(Length = 160, IsNullable = true)]
    public string? Reason { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
