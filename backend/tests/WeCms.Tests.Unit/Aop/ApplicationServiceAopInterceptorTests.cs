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

    private static IAuditedService CreateProxy(ApplicationServiceAopInterceptor interceptor)
    {
        return new ProxyGenerator().CreateInterfaceProxyWithTarget<IAuditedService>(new AuditedService(), interceptor);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var writer = new RecordingAuditWriter();

        services.AddSingleton<IAuditWriter>(writer);
        services.AddSingleton(writer);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<ICacheTenantAccessor>(new StubCacheTenantAccessor("tenant-id"));
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
        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext());
        }
    }

    private sealed class FakeTransactionContext : ITransactionContext
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
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

    private sealed class StubIdGenerator : IIdGenerator
    {
        public string NewId()
        {
            return "trace-id";
        }
    }
}
