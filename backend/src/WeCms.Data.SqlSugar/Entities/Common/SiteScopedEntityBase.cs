using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar.Entities.Common;

public abstract class SiteScopedEntityBase : TenantEntityBase, ISiteScopedEntity
{
    [SugarColumn(ColumnName = "site_id")]
    public long SiteId { get; set; }
}
