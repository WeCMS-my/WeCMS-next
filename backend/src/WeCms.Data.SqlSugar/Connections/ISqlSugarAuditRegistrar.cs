using SqlSugar;

namespace WeCms.Data.SqlSugar;

public interface ISqlSugarAuditRegistrar
{
    void Register(SqlSugarScopeProvider db);
}
