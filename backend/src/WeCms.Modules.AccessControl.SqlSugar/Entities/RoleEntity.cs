using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_role")]
[SugarIndex("ux_sys_role_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_sys_role_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class RoleEntity : EntityBase
{
    [SugarColumn(Length = 64)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_builtin")]
    public bool IsBuiltin { get; set; }

    [SugarColumn(ColumnName = "is_locked")]
    public bool IsLocked { get; set; }
}
