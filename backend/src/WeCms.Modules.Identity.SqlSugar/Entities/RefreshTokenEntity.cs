using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Entities;

[SugarTable("sys_refresh_token")]
[SugarIndex("ux_sys_refresh_token_hash", nameof(TokenHash), OrderByType.Asc, true)]
[SugarIndex("ix_sys_refresh_token_user_id", nameof(UserId), OrderByType.Asc)]
[SugarIndex("ix_sys_refresh_token_family_id", nameof(FamilyId), OrderByType.Asc)]
public sealed class RefreshTokenEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "token_hash", Length = 64)]
    public string TokenHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "family_id", Length = 36)]
    public string FamilyId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "expires_at")]
    public DateTime ExpiresAt { get; set; }

    [SugarColumn(ColumnName = "revoked_at", IsNullable = true)]
    public DateTime? RevokedAt { get; set; }

    [SugarColumn(ColumnName = "replaced_by_token_hash", Length = 64, IsNullable = true)]
    public string? ReplacedByTokenHash { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
