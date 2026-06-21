using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Organization.SqlSugar.Entities;

[SugarTable("sys_dept")]
[SugarIndex("ux_sys_dept_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_sys_dept_parent_id", nameof(ParentId), OrderByType.Asc)]
public sealed class DepartmentEntity : EntityBase
{
    [SugarColumn(ColumnName = "parent_id", IsNullable = true)]
    public long? ParentId { get; set; }

    [SugarColumn(Length = 80)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;
}
