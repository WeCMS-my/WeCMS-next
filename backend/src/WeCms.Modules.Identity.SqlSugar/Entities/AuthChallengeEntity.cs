using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Entities;

[SugarTable("sys_auth_challenge")]
[SugarIndex("ux_sys_auth_challenge_challenge_id", nameof(ChallengeId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_auth_challenge_user_status", nameof(UserId), OrderByType.Asc)]
public sealed class AuthChallengeEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "challenge_id", Length = 43)]
    public string ChallengeId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "challenge_type", Length = 32)]
    public string ChallengeType { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "failed_attempts")]
    public int FailedAttempts { get; set; }

    [SugarColumn(ColumnName = "expires_at")]
    public DateTime ExpiresAt { get; set; }

    [SugarColumn(ColumnName = "consumed_at", IsNullable = true)]
    public DateTime? ConsumedAt { get; set; }

    [SugarColumn(Length = 45, IsNullable = true)]
    public string? Ip { get; set; }

    [SugarColumn(ColumnName = "user_agent", Length = 500, IsNullable = true)]
    public string? UserAgent { get; set; }

    [SugarColumn(ColumnName = "trace_id", Length = 64, IsNullable = true)]
    public string? TraceId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
