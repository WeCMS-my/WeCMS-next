using SqlSugar;

namespace WeCms.Data.SqlSugar;

public interface ISqlSugarClientFactory
{
    ISqlSugarClient Create();

    ISqlSugarClient Create(string connectionName);

    ISqlSugarClient CreateForTenant(long tenantId);
}
