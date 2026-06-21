using SqlSugar;
using WeCms.Data.SqlSugar.Entities.Common;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class EntityBaseContractTests
{
    [Fact]
    public void EntityBase_ImplementsEntityAuditAndSoftDeleteContracts()
    {
        Assert.True(typeof(IEntity<long>).IsAssignableFrom(typeof(TestEntity)));
        Assert.True(typeof(IAuditedEntity).IsAssignableFrom(typeof(TestEntity)));
        Assert.True(typeof(ISoftDeleteEntity).IsAssignableFrom(typeof(TestEntity)));
    }

    [Fact]
    public void TenantEntityBase_ImplementsTenantContract()
    {
        Assert.True(typeof(ITenantEntity).IsAssignableFrom(typeof(TestTenantEntity)));
    }

    [Fact]
    public void SiteScopedEntityBase_ImplementsTenantAndSiteScopedContracts()
    {
        Assert.True(typeof(ITenantEntity).IsAssignableFrom(typeof(TestSiteScopedEntity)));
        Assert.True(typeof(ISiteScopedEntity).IsAssignableFrom(typeof(TestSiteScopedEntity)));
    }

    [Fact]
    public void EntityBase_PrimaryKeyUsesSugarColumnMetadata()
    {
        var property = typeof(TestEntity).GetProperty(nameof(TestEntity.Id));
        Assert.NotNull(property);

        var attribute = Assert.Single(property.GetCustomAttributes(typeof(SugarColumn), inherit: true).Cast<SugarColumn>());
        Assert.True(attribute.IsPrimaryKey);
        Assert.True(attribute.IsIdentity);
        Assert.Equal("id", attribute.ColumnName);
    }

    private sealed class TestEntity : EntityBase;

    private sealed class TestTenantEntity : TenantEntityBase;

    private sealed class TestSiteScopedEntity : SiteScopedEntityBase;
}
