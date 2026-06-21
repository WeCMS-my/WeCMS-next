using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Entities;

[SugarTable("sys_login_failure_counter")]
[SugarIndex("ux_sys_login_failure_counter_scope_target", nameof(Scope), OrderByType.Asc, true)]
[SugarIndex("ix_sys_login_failure_counter_updated_at", nameof(UpdatedAt), OrderByType.Asc)]
public sealed class LoginFailureCounterEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(Length = 32)]
    public string Scope { get; set; } = string.Empty;

    [SugarColumn(Length = 128)]
    public string Target { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "failure_count")]
    public int FailureCount { get; set; }

    [SugarColumn(ColumnName = "window_started_at")]
    public DateTime WindowStartedAt { get; set; }

    [SugarColumn(ColumnName = "last_failed_at")]
    public DateTime LastFailedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
