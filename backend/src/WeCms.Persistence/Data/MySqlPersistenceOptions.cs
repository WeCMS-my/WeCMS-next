using WeCms.Shared;

namespace WeCms.Persistence.Data;

public sealed record MySqlPersistenceOptions(string ConnectionString)
{
    public MySqlPersistenceOptions Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new DomainException(
                ApiCodes.InvalidConfiguration,
                "配置缺失：ConnectionStrings:WeCms",
                500);
        }

        return this;
    }
}
