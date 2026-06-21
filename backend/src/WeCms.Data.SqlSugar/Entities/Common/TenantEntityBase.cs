using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar.Entities.Common;

public abstract class TenantEntityBase : EntityBase, ITenantEntity
{
    [SugarColumn(ColumnName = "tenant_id")]
    public long TenantId { get; set; }
}
