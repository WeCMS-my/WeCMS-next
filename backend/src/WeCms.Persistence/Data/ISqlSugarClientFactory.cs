using SqlSugar;

namespace WeCms.Persistence.Data;

public interface ISqlSugarClientFactory
{
    ISqlSugarClient Create();
}
