using SqlSugar;

namespace WeCms.Modules.FileCenter.SqlSugar.Entities;

[SugarTable("sys_file")]
[SugarIndex("ux_sys_file_object_key", nameof(ObjectKey), OrderByType.Asc, true)]
[SugarIndex("ix_sys_file_created_by", nameof(CreatedBy), OrderByType.Asc)]
[SugarIndex("ix_sys_file_deleted_at", nameof(DeletedAt), OrderByType.Asc)]
public sealed class FileEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "storage_provider", Length = 32)]
    public string StorageProvider { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string Bucket { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "object_key", Length = 160)]
    public string ObjectKey { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "original_name", Length = 255)]
    public string OriginalName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "file_ext", Length = 16)]
    public string FileExt { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "mime_type", Length = 120)]
    public string MimeType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "size_bytes")]
    public long SizeBytes { get; set; }

    [SugarColumn(Length = 64)]
    public string Sha256 { get; set; } = string.Empty;

    [SugarColumn(Length = 32)]
    public string Status { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "created_by")]
    public long CreatedBy { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}
