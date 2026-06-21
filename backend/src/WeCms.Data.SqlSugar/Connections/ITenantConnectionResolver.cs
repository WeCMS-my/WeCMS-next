namespace WeCms.Data.SqlSugar;

public interface ITenantConnectionResolver
{
    DatabaseConnectionOptions Resolve(long tenantId);
}
