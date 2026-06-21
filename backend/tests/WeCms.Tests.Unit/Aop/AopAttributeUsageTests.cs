using WeCms.Aop;

namespace WeCms.Tests.Unit.Aop;

public sealed class AopAttributeUsageTests
{
    [Fact]
    public void UnitOfWorkAttribute_TargetsInterfacesAndMethods()
    {
        AssertAttributeUsage<UnitOfWorkAttribute>(allowMultiple: false);
    }

    [Fact]
    public void CacheableAttribute_TargetsInterfacesAndMethods()
    {
        AssertAttributeUsage<CacheableAttribute>(allowMultiple: false);
        var attribute = new CacheableAttribute("tenant:module:resource");

        Assert.Equal("tenant:module:resource", attribute.KeyTemplate);
    }

    [Fact]
    public void CacheEvictAttribute_TargetsInterfacesAndMethodsAndAllowsMultiple()
    {
        AssertAttributeUsage<CacheEvictAttribute>(allowMultiple: true);
        var attribute = new CacheEvictAttribute("tenant:module:", CacheEvictionMode.Prefix);

        Assert.Equal("tenant:module:", attribute.KeyTemplate);
        Assert.Equal(CacheEvictionMode.Prefix, attribute.Mode);
    }

    [Fact]
    public void AuditedAttribute_TargetsInterfacesAndMethods()
    {
        AssertAttributeUsage<AuditedAttribute>(allowMultiple: false);
        var attribute = new AuditedAttribute("configuration.update");

        Assert.Equal("configuration.update", attribute.Operation);
    }

    [Fact]
    public void AopAttributes_ExposeInterceptorOrder()
    {
        Assert.Equal(0, new UnitOfWorkAttribute().Order);
        Assert.Equal(100, new CacheableAttribute("cache:key").Order);
        Assert.Equal(200, new CacheEvictAttribute("cache:key").Order);
        Assert.Equal(300, new AuditedAttribute().Order);
    }

    private static void AssertAttributeUsage<TAttribute>(bool allowMultiple)
        where TAttribute : Attribute
    {
        var usage = Attribute.GetCustomAttribute(typeof(TAttribute), typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        Assert.NotNull(usage);
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Interface));
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Method));
        Assert.False(usage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.Equal(allowMultiple, usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }
}
