using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.AccessControl.SqlSugar.Entities;

[SugarTable("sys_menu")]
[SugarIndex("ux_sys_menu_name", nameof(Name), OrderByType.Asc, true)]
[SugarIndex("ix_sys_menu_parent_id", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("ix_sys_menu_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class MenuEntity : EntityBase
{
    [SugarColumn(ColumnName = "parent_id", IsNullable = true)]
    public long? ParentId { get; set; }

    [SugarColumn(Length = 32)]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 240)]
    public string Path { get; set; } = string.Empty;

    [SugarColumn(Length = 240, IsNullable = true)]
    public string? Component { get; set; }

    [SugarColumn(Length = 120)]
    public string Title { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "i18n_key", Length = 160, IsNullable = true)]
    public string? I18nKey { get; set; }

    [SugarColumn(Length = 120, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }

    public bool Hidden { get; set; }

    [SugarColumn(ColumnName = "keep_alive")]
    public bool KeepAlive { get; set; }

    [SugarColumn(ColumnName = "external_url", Length = 500, IsNullable = true)]
    public string? ExternalUrl { get; set; }

    [SugarColumn(ColumnName = "permission_code", Length = 160, IsNullable = true)]
    public string? PermissionCode { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_builtin")]
    public bool IsBuiltin { get; set; }
}
