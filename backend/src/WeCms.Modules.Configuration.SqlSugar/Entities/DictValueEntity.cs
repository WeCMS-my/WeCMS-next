using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Configuration.SqlSugar.Entities;

[SugarTable("sys_dict_value")]
[SugarIndex("ux_sys_dict_value_type_value", nameof(TypeId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_dict_value_type_id", nameof(TypeId), OrderByType.Asc)]
[SugarIndex("ix_sys_dict_value_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class DictValueEntity : EntityBase
{
    [SugarColumn(ColumnName = "type_id")]
    public long TypeId { get; set; }

    [SugarColumn(Length = 120)]
    public string Label { get; set; } = string.Empty;

    [SugarColumn(Length = 160)]
    public string Value { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_default")]
    public bool IsDefault { get; set; }
}
