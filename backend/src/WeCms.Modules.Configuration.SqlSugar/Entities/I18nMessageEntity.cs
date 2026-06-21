using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;

namespace WeCms.Modules.Configuration.SqlSugar.Entities;

[SugarTable("sys_i18n_message")]
[SugarIndex("uq_sys_i18n_message_locale_key", nameof(Locale), OrderByType.Asc, true)]
[SugarIndex("ix_sys_i18n_message_locale_status", nameof(Locale), OrderByType.Asc)]
[SugarIndex("ix_sys_i18n_message_module", nameof(Module), OrderByType.Asc)]
[SugarIndex("ix_sys_i18n_message_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class I18nMessageEntity : EntityBase
{
    [SugarColumn(Length = 16)]
    public string Locale { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Module { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "message_key", Length = 160)]
    public string MessageKey { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "message_value", ColumnDataType = "TEXT")]
    public string MessageValue { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;
}
