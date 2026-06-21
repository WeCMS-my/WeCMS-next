using SqlSugar;

namespace WeCms.Modules.Configuration.SqlSugar.Entities;

[SugarTable("sys_setting")]
[SugarIndex("ux_sys_setting_key", nameof(Key), OrderByType.Asc, true)]
[SugarIndex("ix_sys_setting_group_code", nameof(GroupCode), OrderByType.Asc)]
public sealed class SettingEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "key", Length = 120)]
    public string Key { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "value", ColumnDataType = "TEXT", IsNullable = true)]
    public string? Value { get; set; }

    [SugarColumn(ColumnName = "value_type", Length = 32)]
    public string ValueType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "group_code", Length = 80)]
    public string GroupCode { get; set; } = string.Empty;

    [SugarColumn(Length = 120)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "is_sensitive")]
    public bool IsSensitive { get; set; }

    [SugarColumn(ColumnName = "is_system")]
    public bool IsSystem { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
    public long? UpdatedBy { get; set; }
}
