using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Api.RateLimiting;
using WeCms.Api.Security;
using WeCms.Modules.Security;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task SecurityRejectionEventBuffer_AggregatesRateLimitEventsWithinOneMinuteWindow()
    {
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);
        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);

        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now, traceId: "trace-rate-1"))));
        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now.AddSeconds(10), traceId: "trace-rate-2"))));

        var events = await buffer.DrainDueAsync(now.AddMinutes(1).AddSeconds(1), 100);
        Assert.True(events.Length == 1);
        var securityEvent = events[0];
        var rateLimitHit = securityEvent.RateLimitHit;
        Assert.NotNull(rateLimitHit);

        Assert.Equal(SecurityRejectionEventKind.RateLimit, securityEvent.Kind);
        Assert.Equal(2, rateLimitHit.RejectedCount);
        Assert.Equal("trace-rate-2", rateLimitHit.TraceId);
        Assert.Equal("192.168.1.10", rateLimitHit.Ip);
        Assert.Equal("/api/v1/auth/login", rateLimitHit.Path);
        Assert.Equal(RateLimitPolicyNames.AuthLogin, rateLimitHit.Policy);
    }

    [Fact]
    public async Task SecurityRejectionEventBuffer_SeparatesDifferentRateLimitKeys()
    {
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);
        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);

        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now, userId: 7, traceId: "trace-rate-1"))));
        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthRefresh, "/api/v1/auth/login", "192.168.1.10", now, userId: 8, traceId: "trace-rate-2"))));

        var events = await buffer.DrainDueAsync(now.AddMinutes(1).AddSeconds(1), 100);
        Assert.Equal(2, events.Length);
        Assert.Contains(events, @event => @event.RateLimitHit?.UserId == 7);
        Assert.Contains(events, @event => @event.RateLimitHit?.UserId == 8);
    }

    [Fact]
    public async Task SecurityRejectionEventBuffer_SplitsAggregationByOneMinuteWindow()
    {
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);
        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);

        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now, traceId: "trace-rate-1"))));
        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now.AddSeconds(90), traceId: "trace-rate-2"))));

        var firstWindowEvents = await buffer.DrainDueAsync(now.AddMinutes(1).AddSeconds(1), 100);
        Assert.True(firstWindowEvents.Length == 1);
        var firstWindowEvent = firstWindowEvents[0];
        var firstWindowHit = firstWindowEvent.RateLimitHit;
        Assert.NotNull(firstWindowHit);
        Assert.Equal("trace-rate-1", firstWindowHit.TraceId);
        Assert.Equal(1, firstWindowHit.RejectedCount);

        var secondWindowEvents = await buffer.DrainDueAsync(now.AddMinutes(2).AddSeconds(1), 100);
        Assert.True(secondWindowEvents.Length == 1);
        var secondWindowEvent = secondWindowEvents[0];
        var secondWindowHit = secondWindowEvent.RateLimitHit;
        Assert.NotNull(secondWindowHit);
        Assert.Equal("trace-rate-2", secondWindowHit.TraceId);
        Assert.Equal(1, secondWindowHit.RejectedCount);
    }

    [Fact]
    public void SecurityRejectionEventBuffer_GetMetrics_ReturnsDiagnosticsState()
    {
        var buffer = new SecurityRejectionEventBuffer(NullLogger<SecurityRejectionEventBuffer>.Instance);

        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);
        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromRateLimit(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now, traceId: "trace-rate-1"))));
        Assert.True(buffer.TryEnqueue(SecurityRejectionEvent.FromIpAccessDenied(new IpAccessDeniedSecurityEvent("203.0.113.20", "trace-ip", now, 2))));

        var metrics = buffer.GetMetrics();

        Assert.Equal(3, metrics.SecurityRejectionBufferAggregates);
        Assert.Equal(0, metrics.SecurityRejectionBufferDroppedTotal);
        Assert.True(metrics.SecurityRejectionBufferDroppedByKind.TryGetValue("RateLimit", out var rateLimitDropped));
        Assert.Equal(0, rateLimitDropped);
        Assert.True(metrics.SecurityRejectionBufferDroppedByKind.TryGetValue("IpAccessDenied", out var ipDeniedDropped));
        Assert.Equal(0, ipDeniedDropped);
        Assert.Null(metrics.SecurityRejectionBufferLastDropAt);
    }

    [Fact]
    public async Task OnRejectedAsync_RecordsThroughSecurityRejectionBufferWithoutDirectRecorder()
    {
        var clock = new FakeAuthClock(DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture));
        var buffer = new FakeSecurityRejectionEventBuffer();
        var services = new ServiceCollection()
            .AddSingleton<IAuthClock>(clock)
            .AddSingleton<ISecurityRejectionEventBuffer>(buffer)
            .BuildServiceProvider();

        var httpContext = CreateContext("/api/v1/auth/login", "POST", "192.168.1.10");
        httpContext.RequestServices = services;
        httpContext.Response.Body = new MemoryStream();

        await InvokeOnRejectedAsync(httpContext);

        Assert.NotNull(buffer.Record);
        Assert.Equal(SecurityRejectionEventKind.RateLimit, buffer.Record.Kind);
        Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode);
        Assert.Equal("/api/v1/auth/login", buffer.Record.RateLimitHit?.Path);
    }

    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_NormalizesAndPersistsEvent()
    {
        var repository = new FakeRateLimitSecurityEventRepository();
        var alertService = new FakeSecurityAlertService();
        var service = new RateLimitSecurityEventService(repository, alertService);

        await service.RecordHitAsync(
            new RateLimitHitRecord(
                RateLimitPolicyNames.AuthLogin,
                "POST",
                "/api/v1/auth/login",
                42,
                "admin",
                " 192.168.1.10 ",
                "unit-test",
                "trace-rate",
                DateTimeOffset.Parse("2026-06-18T00:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture)),
            CancellationToken.None);

        Assert.NotNull(repository.Record);
        Assert.Equal("rate_limit_hit", repository.Record.EventType);
        Assert.Equal("warning", repository.Record.Severity);
        Assert.Equal("rate-limit", repository.Record.Source);
        Assert.Equal(RateLimitPolicyNames.AuthLogin, repository.Record.Policy);
        Assert.Equal("POST", repository.Record.HttpMethod);
        Assert.Equal("/api/v1/auth/login", repository.Record.Path);
        Assert.Equal("192.168.1.10", repository.Record.Ip);
        Assert.Equal("trace-rate", repository.Record.TraceId);
        Assert.Equal(1, alertService.Count);
    }

    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_IncludesAggregatedRejectedCount()
    {
        var repository = new FakeRateLimitSecurityEventRepository();
        var service = new RateLimitSecurityEventService(repository, new FakeSecurityAlertService());

        await service.RecordHitAsync(
            new RateLimitHitRecord(
                RateLimitPolicyNames.AuthLogin,
                "POST",
                "/api/v1/auth/login",
                null,
                null,
                "192.168.1.10",
                "unit-test",
                "trace-rate",
                DateTimeOffset.Parse("2026-06-18T00:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture),
                RejectedCount: 3),
            CancellationToken.None);

        Assert.NotNull(repository.Record);
        Assert.Contains("Rejected count: 3.", repository.Record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_RejectsUnknownPolicy()
    {
        var service = new RateLimitSecurityEventService(new FakeRateLimitSecurityEventRepository(), new FakeSecurityAlertService());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordHitAsync(
                new RateLimitHitRecord("unknown_policy", "POST", "/api/v1/auth/login", null, null, "127.0.0.1", "unit-test", "trace-rate", DateTimeOffset.UtcNow),
                CancellationToken.None));
    }

    [Fact]
    public void RateLimitPolicyNames_DefinesRequiredH2Policies()
    {
        Assert.Contains("auth_login_policy", RateLimitPolicyNames.All);
        Assert.Contains("auth_refresh_policy", RateLimitPolicyNames.All);
        Assert.Contains("auth_2fa_policy", RateLimitPolicyNames.All);
        Assert.Contains("admin_write_policy", RateLimitPolicyNames.All);
        Assert.Contains("file_upload_policy", RateLimitPolicyNames.All);
        Assert.Contains("security_unban_policy", RateLimitPolicyNames.All);
    }

    [Fact]
    public void UserEndpointPartition_UsesUserIdAcrossPathsAndMethods()
    {
        var partitionById = GetUserEndpointPartition(CreateContext("/api/v1/admin/users", "POST", "10.0.0.1", 99));
        var partitionByIdOnOtherEndpoint = GetUserEndpointPartition(CreateContext("/api/v1/system/menus", "PUT", "10.0.0.1", 99));

        Assert.Equal(partitionById, partitionByIdOnOtherEndpoint);
    }

    [Fact]
    public void UserEndpointPartition_UsesIpForUnauthenticatedClientsAcrossPaths()
    {
        var partitionByIp = GetUserEndpointPartition(CreateContext("/api/v1/files/upload", "POST", "10.0.0.2"));
        var partitionByIpOnOtherEndpoint = GetUserEndpointPartition(CreateContext("/api/v1/security/bans/unban", "DELETE", "10.0.0.2"));

        Assert.Equal(partitionByIp, partitionByIpOnOtherEndpoint);
    }

    [Fact]
    public void UserEndpointPartition_DistinguishesDifferentUsers()
    {
        var firstUserPartition = GetUserEndpointPartition(CreateContext("/api/v1/users", "POST", "10.0.0.3", 7));
        var secondUserPartition = GetUserEndpointPartition(CreateContext("/api/v1/users", "PUT", "10.0.0.3", 8));

        Assert.NotEqual(firstUserPartition, secondUserPartition);
    }

    private static string GetUserEndpointPartition(DefaultHttpContext context)
    {
        var method = typeof(WeCmsRateLimitingExtensions).GetMethod(
            "UserEndpointPartition",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var partition = method.Invoke(null, new object[] { context });
        Assert.IsType<string>(partition);
        return partition as string ?? string.Empty;
    }

    private static DefaultHttpContext CreateContext(
        string path,
        string method,
        string remoteIp,
        long? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        if (userId is not null)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString(CultureInfo.InvariantCulture))],
                authenticationType: "test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private static RateLimitHitRecord CreateHit(
        string policy,
        string path,
        string ip,
        DateTimeOffset now,
        long? userId = null,
        string traceId = "trace-rate")
    {
        return new RateLimitHitRecord(policy, "POST", path, userId, "admin", ip, "unit-test", traceId, now);
    }

    private static async Task InvokeOnRejectedAsync(HttpContext httpContext)
    {
        var method = typeof(WeCmsRateLimitingExtensions).GetMethod(
            "OnRejectedAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(
            null,
            [
                new OnRejectedContext
                {
                    HttpContext = httpContext,
                    Lease = new FakeRateLimitLease()
                },
                CancellationToken.None
            ]);

        var valueTask = Assert.IsType<ValueTask>(result);
        await valueTask;
    }

    private sealed class FakeRateLimitSecurityEventRepository : IRateLimitSecurityEventRepository
    {
        public RateLimitSecurityEventRecord? Record { get; private set; }

        public Task RecordHitAsync(RateLimitSecurityEventRecord record, CancellationToken cancellationToken)
        {
            Record = record;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityAlertService : ISecurityAlertService
    {
        public int Count { get; private set; }

        public Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityRejectionEventBuffer : ISecurityRejectionEventBuffer
    {
        public SecurityRejectionEvent? Record { get; private set; }

        public bool TryEnqueue(SecurityRejectionEvent record)
        {
            Record = record;
            return true;
        }

        public SecurityRejectionSnapshotDto GetSnapshot()
        {
            return new SecurityRejectionSnapshotDto(
                0,
                0,
                null,
                new Dictionary<string, long>());
        }

        public SecurityRejectionMetricsDto GetMetrics()
        {
            return new SecurityRejectionMetricsDto(
                0,
                0,
                new Dictionary<string, long>(),
                null);
        }
    }

    private sealed class FakeAuthClock : IAuthClock
    {
        public FakeAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeRateLimitLease : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
