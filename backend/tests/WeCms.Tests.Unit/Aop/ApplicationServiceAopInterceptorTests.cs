using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Aop;
using WeCms.Caching;
using WeCms.Shared.Data;
using WeCms.Shared.Endpoints;
using WeCms.Shared.Id;
using Xunit;

namespace WeCms.Tests.Unit.Aop;

public sealed class ApplicationServiceAopInterceptorTests
{
    [Fact]
    public async Task ApplicationServiceAopInterceptor_WritesAudit_OnSuccess()
    {
        using var provider = CreateProvider();
        var writer = provider.GetRequiredService<RecordingAuditWriter>();
        var interceptor = provider.GetRequiredService<ApplicationServiceAopInterceptor>();
        var proxy = CreateProxy(interceptor);

        await proxy.CreateAsync();

        Assert.Collection(
            writer.Records,
            first => AssertAudit(first, AuditWriteStatus.Started, "identity", "users", "create", string.Empty),
            second => AssertAudit(second, AuditWriteStatus.Completed, "identity", "users", "create", string.Empty));
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_WritesAudit_OnFailure()
    {
        using var provider = CreateProvider();
        var writer = provider.GetRequiredService<RecordingAuditWriter>();
        var interceptor = provider.GetRequiredService<ApplicationServiceAopInterceptor>();
        var proxy = CreateProxy(interceptor);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(proxy.FailAsync);

        Assert.Equal("service failure", exception.Message);
        Assert.Collection(
            writer.Records,
            first => AssertAudit(first, AuditWriteStatus.Started, "identity", "users", "fail", string.Empty),
            second =>
            {
                AssertAudit(second, AuditWriteStatus.Failed, "identity", "users", "fail", "service failure");
                Assert.Equal("service failure", second.Detail);
            });
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_SkipsAudit_WhenNoAuditedAttribute()
    {
        using var provider = CreateProvider();
        var writer = provider.GetRequiredService<RecordingAuditWriter>();
        var interceptor = provider.GetRequiredService<ApplicationServiceAopInterceptor>();
        var proxy = CreateProxy(interceptor);

        await proxy.PlainAsync();

        Assert.Empty(writer.Records);
    }

    [Fact]
    public void ApplicationServiceAopInterceptor_RejectsVoidMethodsWithoutExecutingTarget()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<ApplicationServiceAopInterceptor>();
        var target = new SyncService();
        var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<ISyncService>(target, interceptor);

        var exception = Assert.Throws<NotSupportedException>(proxy.Run);

        Assert.Contains("Task or Task<T>", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, target.RunCount);
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_CommitsUnitOfWork_OnSuccess()
    {
        using var provider = CreateProvider();
        var unitOfWork = provider.GetRequiredService<FakeUnitOfWork>();
        var proxy = CreatePipelineProxy(provider, new PipelineService());

        await proxy.SaveAsync();

        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_RollsBackUnitOfWork_AndPreservesException()
    {
        using var provider = CreateProvider();
        var unitOfWork = provider.GetRequiredService<FakeUnitOfWork>();
        var target = new PipelineService();
        var proxy = CreatePipelineProxy(provider, target);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(proxy.FailInTransactionAsync);

        Assert.Same(target.Failure, exception);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_ReturnsCachedValueWithoutExecutingTarget_OnCacheHit()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cacheInterceptor = provider.GetRequiredService<CacheInterceptor>();
        var target = new PipelineService();
        var proxy = CreatePipelineProxy(provider, target);
        var key = cacheInterceptor.BuildKey(
            new CacheableAttribute("identity:users:detail"),
            new CacheInvocationContext("tenant-id", [7L]));
        await cache.SetAsync(key, "cached-user", cancellationToken: TestContext.Current.CancellationToken);

        var result = await proxy.GetCachedUserAsync(7);

        Assert.Equal("cached-user", result);
        Assert.Equal(0, target.GetCachedUserCalls);
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_EvictsCacheKeyAfterSuccessfulTaskOfT()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cacheInterceptor = provider.GetRequiredService<CacheInterceptor>();
        var target = new PipelineService();
        var proxy = CreatePipelineProxy(provider, target);
        var key = cacheInterceptor.BuildKey(
            new CacheEvictAttribute("identity:users:detail"),
            new CacheInvocationContext("tenant-id", [7L]));
        await cache.SetAsync(key, "stale-user", cancellationToken: TestContext.Current.CancellationToken);

        var result = await proxy.UpdateUserAsync(7);

        Assert.Equal("updated-7", result);
        Assert.Equal(1, target.UpdateUserCalls);
        Assert.Null(await cache.GetAsync<string>(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_EvictsCachePrefixAfterSuccessfulTask()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cacheInterceptor = provider.GetRequiredService<CacheInterceptor>();
        var proxy = CreatePipelineProxy(provider, new PipelineService());
        var attribute = new CacheEvictAttribute("identity:users:list", CacheEvictionMode.Prefix);
        var firstKey = cacheInterceptor.BuildKey(attribute, new CacheInvocationContext("tenant-id", [1L]));
        var secondKey = cacheInterceptor.BuildKey(attribute, new CacheInvocationContext("tenant-id", [2L]));
        var otherTenantKey = cacheInterceptor.BuildKey(attribute, new CacheInvocationContext("other-tenant", [1L]));
        await cache.SetAsync(firstKey, "first", cancellationToken: TestContext.Current.CancellationToken);
        await cache.SetAsync(secondKey, "second", cancellationToken: TestContext.Current.CancellationToken);
        await cache.SetAsync(otherTenantKey, "other", cancellationToken: TestContext.Current.CancellationToken);

        await proxy.RefreshUsersAsync(1);

        Assert.Null(await cache.GetAsync<string>(firstKey, TestContext.Current.CancellationToken));
        Assert.Null(await cache.GetAsync<string>(secondKey, TestContext.Current.CancellationToken));
        Assert.Equal("other", await cache.GetAsync<string>(otherTenantKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_UsesTenantAccessorForCacheKeys()
    {
        using var provider = CreateProvider("tenant-77");
        var cache = provider.GetRequiredService<ICache>();
        var cacheInterceptor = provider.GetRequiredService<CacheInterceptor>();
        var proxy = CreatePipelineProxy(provider, new PipelineService());

        var result = await proxy.GetCachedUserAsync(11);

        var key = cacheInterceptor.BuildKey(
            new CacheableAttribute("identity:users:detail"),
            new CacheInvocationContext("tenant-77", [11L]));
        Assert.Equal("loaded-11", result);
        Assert.Equal("loaded-11", await cache.GetAsync<string>(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplicationServiceAopInterceptor_PreservesTaskOfTResult()
    {
        using var provider = CreateProvider();
        var proxy = CreatePipelineProxy(provider, new PipelineService());

        var result = await proxy.SaveAndReturnAsync();

        Assert.Equal("saved", result);
    }

    [Fact]
    public void ApplicationServiceAopInterceptor_DoesNotWrapSynchronousGenericPipelineException()
    {
        using var provider = CreateProvider(cacheTenantAccessor: new ThrowingCacheTenantAccessor());
        var proxy = CreatePipelineProxy(provider, new PipelineService());

        var exception = Assert.Throws<InvalidOperationException>(() => InvokeCachedUserSynchronously(proxy));

        Assert.Equal("tenant accessor failure", exception.Message);
    }

    private static void InvokeCachedUserSynchronously(IPipelineService proxy)
    {
        proxy.GetCachedUserAsync(11).GetAwaiter().GetResult();
    }

    private static IAuditedService CreateProxy(ApplicationServiceAopInterceptor interceptor)
    {
        return new ProxyGenerator().CreateInterfaceProxyWithTarget<IAuditedService>(new AuditedService(), interceptor);
    }

    private static IPipelineService CreatePipelineProxy(ServiceProvider provider, PipelineService target)
    {
        return new ProxyGenerator().CreateInterfaceProxyWithTarget<IPipelineService>(
            target,
            provider.GetRequiredService<ApplicationServiceAopInterceptor>());
    }

    private static ServiceProvider CreateProvider(string tenantId = "tenant-id", ICacheTenantAccessor? cacheTenantAccessor = null)
    {
        var services = new ServiceCollection();
        var writer = new RecordingAuditWriter();
        var unitOfWork = new FakeUnitOfWork();

        services.AddSingleton<IAuditWriter>(writer);
        services.AddSingleton(writer);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton(unitOfWork);
        services.AddSingleton(cacheTenantAccessor ?? new StubCacheTenantAccessor(tenantId));
        services.AddSingleton<IIdGenerator>(new StubIdGenerator());
        services.AddWeCmsCaching(options =>
        {
            options.ApplicationName = "wecms";
            options.EnvironmentName = "unit";
            options.Version = "v1";
        });
        services.AddSingleton<TransactionInterceptor>();
        services.AddSingleton<CacheInterceptor>();
        services.AddSingleton<ApplicationServiceAopInterceptor>();

        return services.BuildServiceProvider();
    }

    private static void AssertAudit(
        AuditWriteRecord record,
        AuditWriteStatus status,
        string module,
        string resource,
        string action,
        string detail)
    {
        Assert.Equal(module, record.Module);
        Assert.Equal(resource, record.Resource);
        Assert.Equal(action, record.Action);
        Assert.Equal(status, record.Status);
        Assert.Equal("SERVICE", record.RequestMethod);
        Assert.Contains($".{action}", record.RequestPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(detail, record.Detail);
        Assert.False(string.IsNullOrWhiteSpace(record.TraceId));
    }

    public interface IAuditedService
    {
        [Audited("identity.users.create")]
        Task CreateAsync();

        [Audited("identity.users.fail")]
        Task FailAsync();

        Task PlainAsync();
    }

    public interface ISyncService
    {
        [Audited("identity.users.sync")]
        void Run();
    }

    public interface IPipelineService
    {
        [UnitOfWork]
        Task SaveAsync();

        [UnitOfWork]
        Task<string> SaveAndReturnAsync();

        [UnitOfWork]
        Task FailInTransactionAsync();

        [Cacheable("identity:users:detail")]
        Task<string> GetCachedUserAsync(long id);

        [CacheEvict("identity:users:detail")]
        Task<string> UpdateUserAsync(long id);

        [CacheEvict("identity:users:list", CacheEvictionMode.Prefix)]
        Task RefreshUsersAsync(long id);
    }

    public sealed class AuditedService : IAuditedService
    {
        public Task CreateAsync()
        {
            return Task.CompletedTask;
        }

        public Task FailAsync()
        {
            throw new InvalidOperationException("service failure");
        }

        public Task PlainAsync()
        {
            return Task.CompletedTask;
        }
    }

    public sealed class SyncService : ISyncService
    {
        public int RunCount { get; private set; }

        public void Run()
        {
            RunCount++;
        }
    }

    public sealed class PipelineService : IPipelineService
    {
        public InvalidOperationException Failure { get; } = new("pipeline failure");
        public int GetCachedUserCalls { get; private set; }
        public int UpdateUserCalls { get; private set; }

        public Task SaveAsync()
        {
            return Task.CompletedTask;
        }

        public Task<string> SaveAndReturnAsync()
        {
            return Task.FromResult("saved");
        }

        public Task FailInTransactionAsync()
        {
            return Task.FromException(Failure);
        }

        public Task<string> GetCachedUserAsync(long id)
        {
            GetCachedUserCalls++;
            return Task.FromResult($"loaded-{id}");
        }

        public Task<string> UpdateUserAsync(long id)
        {
            UpdateUserCalls++;
            return Task.FromResult($"updated-{id}");
        }

        public Task RefreshUsersAsync(long id)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditWriteRecord> Records { get; } = [];

        public ValueTask WriteAsync(AuditWriteRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int BeginCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginCalls++;
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(this));
        }

        private sealed class FakeTransactionContext(FakeUnitOfWork unitOfWork) : ITransactionContext
        {
            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                unitOfWork.CommitCalls++;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                unitOfWork.RollbackCalls++;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class StubCacheTenantAccessor(string tenantId) : ICacheTenantAccessor
    {
        private readonly string _tenantId = tenantId;

        public string GetCurrentTenantId()
        {
            return _tenantId;
        }
    }

    private sealed class ThrowingCacheTenantAccessor : ICacheTenantAccessor
    {
        public string GetCurrentTenantId()
        {
            throw new InvalidOperationException("tenant accessor failure");
        }
    }

    private sealed class StubIdGenerator : IIdGenerator
    {
        public string NewId()
        {
            return "trace-id";
        }
    }
}
