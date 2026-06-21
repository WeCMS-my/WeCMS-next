using SqlSugar;

namespace WeCms.Data.SqlSugar;

public interface ISqlSugarQueryFilterRegistrar
{
    void Register(SqlSugarScopeProvider db);
}
