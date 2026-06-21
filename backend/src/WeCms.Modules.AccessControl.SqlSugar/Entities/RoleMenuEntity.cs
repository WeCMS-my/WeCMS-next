using SqlSugar;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_role_menu")]
public sealed class RoleMenuEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "role_id")]
    public long RoleId { get; set; }

    [SugarColumn(IsPrimaryKey = true, ColumnName = "menu_id")]
    public long MenuId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}
