using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Entities;

[SugarTable("sys_user_two_factor")]
[SugarIndex("ux_sys_user_two_factor_user_id", nameof(UserId), OrderByType.Asc, true)]
public sealed class UserTwoFactorEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    public bool Enabled { get; set; }

    [SugarColumn(ColumnName = "secret_cipher", ColumnDataType = "TEXT")]
    public string SecretEncrypted { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "confirmed_at", IsNullable = true)]
    public DateTime? ConfirmedAt { get; set; }

    [SugarColumn(ColumnName = "last_totp_step", IsNullable = true)]
    public long? LastTotpStep { get; set; }

    [SugarColumn(ColumnName = "recovery_codes_hash_json", ColumnDataType = "JSON")]
    public string RecoveryCodesHashJson { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "recovery_codes_used_count")]
    public int RecoveryCodesUsedCount { get; set; }

    [SugarColumn(ColumnName = "reset_required")]
    public bool ResetRequired { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
