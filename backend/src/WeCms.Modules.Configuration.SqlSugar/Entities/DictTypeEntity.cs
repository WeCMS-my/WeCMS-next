using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Configuration.SqlSugar.Entities;

[SugarTable("sys_dict_type")]
[SugarIndex("ux_sys_dict_type_code", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("ix_sys_dict_type_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class DictTypeEntity : EntityBase
{
    [SugarColumn(Length = 80)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "is_system")]
    public bool IsSystem { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }
}
