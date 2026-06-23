using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Identity.SqlSugar.Entities;

[SugarTable("sys_user")]
[SugarIndex("ux_sys_user_username", nameof(Username), OrderByType.Asc, true)]
[SugarIndex("ux_sys_user_email", nameof(Email), OrderByType.Asc, true)]
[SugarIndex("ux_sys_user_phone", nameof(Phone), OrderByType.Asc, true)]
[SugarIndex("ix_sys_user_dept_id", nameof(DeptId), OrderByType.Asc)]
[SugarIndex("ix_sys_user_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class UserEntity : EntityBase
{
    [SugarColumn(Length = 64)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "display_name", Length = 120)]
    public string DisplayName { get; set; } = string.Empty;

    [SugarColumn(Length = 160, IsNullable = true)]
    public string? Email { get; set; }

    [SugarColumn(Length = 40, IsNullable = true)]
    public string? Phone { get; set; }

    [SugarColumn(ColumnName = "password_hash", Length = 512)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "dept_id", IsNullable = true)]
    public long? DeptId { get; set; }

    [SugarColumn(ColumnName = "last_login_at", IsNullable = true)]
    public DateTime? LastLoginAt { get; set; }

    [SugarColumn(ColumnName = "last_login_ip", Length = 64, IsNullable = true)]
    public string? LastLoginIp { get; set; }

    [SugarColumn(ColumnName = "must_change_password")]
    public bool MustChangePassword { get; set; }

    [SugarColumn(ColumnName = "security_stamp", Length = 64)]
    public string SecurityStamp { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "permission_version")]
    public long PermissionVersion { get; set; }

    [SugarColumn(ColumnName = "avatar_object_key", Length = 255, IsNullable = true)]
    public string? AvatarObjectKey { get; set; }

    [SugarColumn(ColumnName = "avatar_mime_type", Length = 120, IsNullable = true)]
    public string? AvatarMimeType { get; set; }

    [SugarColumn(ColumnName = "avatar_file_ext", Length = 16, IsNullable = true)]
    public string? AvatarFileExt { get; set; }

    [SugarColumn(ColumnName = "avatar_updated_at", IsNullable = true)]
    public DateTime? AvatarUpdatedAt { get; set; }
}
