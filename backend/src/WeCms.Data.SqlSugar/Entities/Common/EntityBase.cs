using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar.Entities.Common;

public abstract class EntityBase : IEntity<long>, ISoftDeleteEntity, IAuditedEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
    public DateTime? DeletedAt { get; set; }
}
