using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public void Constructor_ShouldExposeDatabaseClient_WhenFactoryCreatesClient()
    {
        var options = new MySqlPersistenceOptions("Server=localhost;Database=wecms_next;");
        var factory = new SqlSugarClientFactory(options);

        using var unitOfWork = new SqlSugarUnitOfWork(factory);

        Assert.NotNull(unitOfWork.Client);
    }
}
