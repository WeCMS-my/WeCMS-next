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
    public async Task FlushBatchAsync_PersistsAggregatedRateLimitEventAsSingleRecord()
    {
        var rateLimitService = new FakeRateLimitSecurityEventService();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IRateLimitSecurityEventService>(rateLimitService)
            .BuildServiceProvider();
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        Assert.True(buffer.TryEnqueue(
            SecurityRejectionEvent.FromRateLimit(CreateRateLimitHit("trace-rate-1", now))));
        Assert.True(buffer.TryEnqueue(
            SecurityRejectionEvent.FromRateLimit(CreateRateLimitHit("trace-rate-2", now.AddSeconds(10)))));
        var events = await buffer.DrainDueAsync(now.AddMinutes(1).AddSeconds(1), 100);
        Assert.Single(events);

        var service = new SecurityRejectionEventFlushHostedService(
            buffer,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SecurityRejectionEventFlushHostedService>.Instance);

        await InvokeFlushBatchAsync(
            service,
            events);

        var record = Assert.Single(rateLimitService.Records);
        Assert.Equal(2, record.RejectedCount);
        Assert.Equal(RateLimitPolicyNames.AuthLogin, record.Policy);
        Assert.Equal("/api/v1/auth/login", record.Path);
    }

    [Fact]
    public async Task FlushBatchAsync_DoesNotAggregateAcrossDifferentWindowStarts()
    {
        var rateLimitService = new FakeRateLimitSecurityEventService();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IRateLimitSecurityEventService>(rateLimitService)
            .BuildServiceProvider();
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        Assert.True(buffer.TryEnqueue(
            SecurityRejectionEvent.FromRateLimit(CreateRateLimitHit("trace-rate-1", now))));
        Assert.True(buffer.TryEnqueue(
            SecurityRejectionEvent.FromRateLimit(CreateRateLimitHit("trace-rate-2", now.AddMinutes(1).AddMilliseconds(500)))));

        var events = await buffer.DrainDueAsync(now.AddMinutes(2).AddSeconds(1), 100);
        Assert.Equal(2, events.Length);

        var service = new SecurityRejectionEventFlushHostedService(
            buffer,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SecurityRejectionEventFlushHostedService>.Instance);

        await InvokeFlushBatchAsync(
            service,
            events);

        Assert.Equal(2, rateLimitService.Records.Count);
        Assert.Equal(1, rateLimitService.Records[0].RejectedCount);
        Assert.Equal("trace-rate-1", rateLimitService.Records[0].TraceId);
        Assert.Equal(1, rateLimitService.Records[1].RejectedCount);
        Assert.Equal("trace-rate-2", rateLimitService.Records[1].TraceId);
    }

    private static RateLimitHitRecord CreateRateLimitHit(string traceId, DateTimeOffset createdAt)
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
            createdAt);
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
}
