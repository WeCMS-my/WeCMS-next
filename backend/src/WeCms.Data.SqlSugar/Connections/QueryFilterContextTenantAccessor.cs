using System.Globalization;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public sealed class QueryFilterContextTenantAccessor : ICacheTenantAccessor
{
    private const string GlobalTenant = "global";
    private readonly IQueryFilterContextAccessor _contextAccessor;

    public QueryFilterContextTenantAccessor(IQueryFilterContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    public string GetCurrentTenantId()
    {
        var tenantId = _contextAccessor.Current.TenantId;
        return tenantId.HasValue
            ? tenantId.Value.ToString(CultureInfo.InvariantCulture)
            : GlobalTenant;
    }
}
