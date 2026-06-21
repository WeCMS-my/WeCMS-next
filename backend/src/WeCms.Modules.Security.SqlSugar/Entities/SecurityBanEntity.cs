using SqlSugar;

namespace WeCms.Modules.Security.SqlSugar.Entities;

[SugarTable("sys_security_ban")]
[SugarIndex("ix_sys_security_ban_lookup", nameof(BanType), OrderByType.Asc)]
[SugarIndex("ix_sys_security_ban_revoked_by", nameof(RevokedBy), OrderByType.Asc)]
public sealed class SecurityBanEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "ban_type", Length = 32)]
    public string BanType { get; set; } = string.Empty;

    [SugarColumn(Length = 128)]
    public string Target { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string Reason { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Severity { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "expires_at", IsNullable = true)]
    public DateTime? ExpiresAt { get; set; }

    [SugarColumn(ColumnName = "revoked_at", IsNullable = true)]
    public DateTime? RevokedAt { get; set; }

    [SugarColumn(ColumnName = "revoked_by", IsNullable = true)]
    public long? RevokedBy { get; set; }

    [SugarColumn(ColumnName = "revoke_reason", Length = 500, IsNullable = true)]
    public string? RevokeReason { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
