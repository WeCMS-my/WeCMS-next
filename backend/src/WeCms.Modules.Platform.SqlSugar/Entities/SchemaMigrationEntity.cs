using SqlSugar;

namespace WeCms.Modules.Platform.SqlSugar.Entities;

[SugarTable("sys_schema_migration")]
public sealed class SchemaMigrationEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "version", Length = 64)]
    public string Version { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string Checksum { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "applied_at")]
    public DateTime AppliedAt { get; set; }
}
