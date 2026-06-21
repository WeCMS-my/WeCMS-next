using SqlSugar;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_role_permission")]
public sealed class RolePermissionEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "role_id")]
    public long RoleId { get; set; }

    [SugarColumn(IsPrimaryKey = true, ColumnName = "permission_id")]
    public long PermissionId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
