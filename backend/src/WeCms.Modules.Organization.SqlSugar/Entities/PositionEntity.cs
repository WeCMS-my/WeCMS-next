using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Organization.SqlSugar.Entities;

[SugarTable("sys_position")]
[SugarIndex("ux_sys_position_code", nameof(Code), OrderByType.Asc, true)]
public sealed class PositionEntity : EntityBase
{
    [SugarColumn(Length = 80)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;
}
