using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_permission")]
[SugarIndex("ux_sys_permission_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_sys_permission_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class PermissionEntity : EntityBase
{
    [SugarColumn(Length = 160)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 160)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string Module { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_builtin")]
    public bool IsBuiltin { get; set; }
}
