using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Api.Security;
using WeCms.Modules.Security;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class SecurityRejectionEventFlushHostedServiceTests
{
    [Fact]
    public async Task FlushBatchAsync_AggregatesRateLimitRejectedCount()
    {
        var rateLimitService = new FakeRateLimitSecurityEventService();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IRateLimitSecurityEventService>(rateLimitService)
            .BuildServiceProvider();
        var service = new SecurityRejectionEventFlushHostedService(
            new EmptySecurityRejectionEventReader(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SecurityRejectionEventFlushHostedService>.Instance);
        var first = CreateRateLimitHit("trace-rate-1");
        var second = CreateRateLimitHit("trace-rate-2");

        await InvokeFlushBatchAsync(
            service,
            [
                SecurityRejectionEvent.FromRateLimit(first),
                SecurityRejectionEvent.FromRateLimit(second)
            ]);

        var record = Assert.Single(rateLimitService.Records);
        Assert.Equal(2, record.RejectedCount);
        Assert.Equal(first.Policy, record.Policy);
        Assert.Equal(first.Path, record.Path);
    }

    private static RateLimitHitRecord CreateRateLimitHit(string traceId)
    {
        return new RateLimitHitRecord(
            RateLimitPolicyNames.AuthLogin,
            "POST",
            "/api/v1/auth/login",
            42,
            "admin",
            "192.168.1.10",
            "unit-test",
            traceId,
            new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero));
    }

    private static async Task InvokeFlushBatchAsync(
        SecurityRejectionEventFlushHostedService service,
        IReadOnlyList<SecurityRejectionEvent> events)
    {
        var method = typeof(SecurityRejectionEventFlushHostedService).GetMethod(
            "FlushBatchAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = Assert.IsAssignableFrom<Task>(method.Invoke(service, [events, CancellationToken.None]));
        await task;
    }

    private sealed class FakeRateLimitSecurityEventService : IRateLimitSecurityEventService
    {
        public List<RateLimitHitRecord> Records { get; } = [];

        public Task RecordHitAsync(RateLimitHitRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class EmptySecurityRejectionEventReader : ISecurityRejectionEventReader
    {
        public async IAsyncEnumerable<SecurityRejectionEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public bool TryRead(out SecurityRejectionEvent record)
        {
            record = null!;
            return false;
        }
    }
}
