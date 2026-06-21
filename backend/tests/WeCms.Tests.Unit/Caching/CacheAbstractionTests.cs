using System.Reflection;
using WeCms.Caching;

namespace WeCms.Tests.Unit.Caching;

public sealed class CacheAbstractionTests
{
    [Fact]
    public void CacheContracts_DefineAsyncFirstApis()
    {
        AssertAsyncMethod<ICache>(nameof(ICache.GetAsync));
        AssertAsyncMethod<ICache>(nameof(ICache.SetAsync));
        AssertAsyncMethod<ICache>(nameof(ICache.RemoveAsync));
        AssertAsyncMethod<ICache>(nameof(ICache.GetOrCreateAsync));
        AssertAsyncMethod<ICache>(nameof(ICache.RemoveByPrefixAsync));
    }

    [Fact]
    public void CacheKeyBuilder_IncludesAppEnvironmentTenantModuleResourceAndVersion()
    {
        var builder = new DefaultCacheKeyBuilder(new CacheOptions
        {
            ApplicationName = "wecms",
            EnvironmentName = "test",
            Version = "v1"
        });

        var key = builder.Build(new CacheKeyParts(
            Tenant: "tenant-42",
            Module: "configuration",
            Resource: "settings",
            Identifier: "site-home"));

        Assert.Equal("wecms:test:tenant-42:configuration:settings:v1:site-home", key);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CacheKeyBuilder_FailsFastWhenRequiredDimensionIsMissing(string missingValue)
    {
        var builder = new DefaultCacheKeyBuilder(new CacheOptions
        {
            ApplicationName = "wecms",
            EnvironmentName = "test",
            Version = "v1"
        });

        Assert.Throws<ArgumentException>(() => builder.Build(new CacheKeyParts(
            Tenant: "tenant-42",
            Module: missingValue,
            Resource: "settings",
            Identifier: "site-home")));
    }

    [Fact]
    public void SystemTextJsonCacheSerializer_RoundTripsDto()
    {
        var serializer = new SystemTextJsonCacheSerializer();
        var value = new CacheSerializationSample("alpha", 7);

        var bytes = serializer.Serialize(value);
        var roundTrip = serializer.Deserialize<CacheSerializationSample>(bytes);

        Assert.Equal(value, roundTrip);
    }

    [Fact]
    public void CacheInvalidator_DefinesPrefixInvalidationAsync()
    {
        var method = typeof(ICacheInvalidator).GetMethod(nameof(ICacheInvalidator.RemoveByPrefixAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(ValueTask), method.ReturnType);
        Assert.Contains(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public void CacheEntryOptions_DefaultsDoNotCacheNullValues()
    {
        var options = new CacheEntryOptions();

        Assert.False(options.CacheNullValues);
    }

    private static void AssertAsyncMethod<TContract>(string methodName)
    {
        var methods = typeof(TContract).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == methodName)
            .ToArray();

        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
        {
            var returnType = method.ReturnType;
            var isValueTask = returnType == typeof(ValueTask);
            var isGenericValueTask = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);

            Assert.True(isValueTask || isGenericValueTask, $"{typeof(TContract).Name}.{method.Name} must return ValueTask.");
        });
    }

    private sealed record CacheSerializationSample(string Name, int Count);
}
