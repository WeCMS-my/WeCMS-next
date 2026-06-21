using SqlSugar;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_user_role")]
public sealed class UserRoleEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(IsPrimaryKey = true, ColumnName = "role_id")]
    public long RoleId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
