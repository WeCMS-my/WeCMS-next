using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Api.RateLimiting;
using WeCms.Modules.Security;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class RateLimitingTests
{
    [Fact]
    public void RateLimitHitBuffer_AggregatesSameKeyWithinWindow()
    {
        var buffer = new InMemoryRateLimitHitBuffer(
            new RateLimitHitBufferOptions(
                TimeSpan.FromMinutes(1),
                MaxAggregateKeys: 16));
        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);

        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now)));
        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now.AddSeconds(10))));

        var summaries = buffer.DrainDue(now.AddMinutes(1));

        var summary = Assert.Single(summaries);
        Assert.Equal(2, summary.HitCount);
        Assert.Equal(RateLimitPolicyNames.AuthLogin, summary.Policy);
        Assert.Equal("/api/v1/auth/login", summary.Path);
        Assert.Equal("192.168.1.10", summary.Ip);
    }

    [Fact]
    public void RateLimitHitBuffer_KeepsDifferentKeysSeparate()
    {
        var buffer = new InMemoryRateLimitHitBuffer(
            new RateLimitHitBufferOptions(
                TimeSpan.FromMinutes(1),
                MaxAggregateKeys: 16));
        var now = DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture);

        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", now)));
        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthRefresh, "/api/v1/auth/refresh", "192.168.1.10", now)));

        var summaries = buffer.DrainDue(now.AddMinutes(1));

        Assert.Equal(2, summaries.Count);
        Assert.Contains(summaries, item => item.Policy == RateLimitPolicyNames.AuthLogin);
        Assert.Contains(summaries, item => item.Policy == RateLimitPolicyNames.AuthRefresh);
    }

    [Fact]
    public async Task RateLimitSecurityEventFlushService_OpensCircuitAfterRepeatedFailuresAndRecovers()
    {
        var clock = new FakeSecurityClock(DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture));
        var buffer = new InMemoryRateLimitHitBuffer(new RateLimitHitBufferOptions(TimeSpan.Zero, MaxAggregateKeys: 16));
        var recorder = new FailingThenRecordingRateLimitSecurityEventService(failuresBeforeSuccess: 3);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IRateLimitSecurityEventService>(recorder)
            .BuildServiceProvider();
        var service = new RateLimitSecurityEventFlushHostedService(
            buffer,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            clock,
            new RateLimitSecurityEventFlushOptions(
                FlushInterval: TimeSpan.FromMinutes(5),
                MaxBatchSize: 100,
                FailureThreshold: 2,
                CircuitBreakerCooldown: TimeSpan.FromMinutes(1)),
            NullLogger<RateLimitSecurityEventFlushHostedService>.Instance);

        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", clock.UtcNow)));
        await service.FlushDueAsync(CancellationToken.None);
        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", clock.UtcNow)));
        await service.FlushDueAsync(CancellationToken.None);

        Assert.True(service.CircuitBreakerOpen);
        Assert.Equal(2, recorder.Attempts);

        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", clock.UtcNow)));
        await service.FlushDueAsync(CancellationToken.None);
        Assert.Equal(2, recorder.Attempts);

        clock.Advance(TimeSpan.FromMinutes(1));
        await service.FlushDueAsync(CancellationToken.None);
        Assert.Equal(3, recorder.Attempts);

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True(buffer.TryRecord(CreateHit(RateLimitPolicyNames.AuthLogin, "/api/v1/auth/login", "192.168.1.10", clock.UtcNow)));
        await service.FlushDueAsync(CancellationToken.None);

        Assert.False(service.CircuitBreakerOpen);
        Assert.Equal(4, recorder.Attempts);
        Assert.Single(recorder.Records);
    }

    [Fact]
    public async Task OnRejectedAsync_RecordsThroughBufferWithoutDirectRecorder()
    {
        var buffer = new RecordingRateLimitHitBuffer();
        var services = new ServiceCollection()
            .AddSingleton<ISecurityClock>(new FakeSecurityClock(DateTimeOffset.Parse("2026-06-23T00:00:00Z", CultureInfo.InvariantCulture)))
            .AddSingleton<IRateLimitHitBuffer>(buffer)
            .BuildServiceProvider();
        var httpContext = CreateContext("/api/v1/auth/login", "POST", "192.168.1.10");
        httpContext.RequestServices = services;
        httpContext.Response.Body = new MemoryStream();

        await InvokeOnRejectedAsync(httpContext);

        Assert.NotNull(buffer.Record);
        Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode);
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
        long? userId = null)
    {
        return new RateLimitHitRecord(policy, "POST", path, userId, "admin", ip, "unit-test", "trace-rate", now);
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

    private sealed class RecordingRateLimitHitBuffer : IRateLimitHitBuffer
    {
        public RateLimitHitRecord? Record { get; private set; }

        public bool TryRecord(RateLimitHitRecord record)
        {
            Record = record;
            return true;
        }

        public IReadOnlyList<RateLimitHitSummary> DrainDue(DateTimeOffset now, int maxItems)
        {
            return [];
        }
    }

    private sealed class FailingThenRecordingRateLimitSecurityEventService : IRateLimitSecurityEventService
    {
        private readonly int _failuresBeforeSuccess;

        public FailingThenRecordingRateLimitSecurityEventService(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Attempts { get; private set; }
        public List<RateLimitHitRecord> Records { get; } = [];

        public Task RecordHitAsync(RateLimitHitRecord record, CancellationToken cancellationToken)
        {
            Attempts++;
            if (Attempts <= _failuresBeforeSuccess)
            {
                throw new InvalidOperationException("simulated recorder failure");
            }

            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityClock : ISecurityClock
    {
        public FakeSecurityClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; private set; }

        public void Advance(TimeSpan value)
        {
            UtcNow = UtcNow.Add(value);
        }
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
