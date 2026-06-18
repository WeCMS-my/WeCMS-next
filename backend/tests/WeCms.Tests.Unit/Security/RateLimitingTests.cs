using WeCms.Modules.System.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_NormalizesAndPersistsEvent()
    {
        var repository = new FakeRateLimitSecurityEventRepository();
        var service = new RateLimitSecurityEventService(repository);

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
    }

    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_RejectsUnknownPolicy()
    {
        var service = new RateLimitSecurityEventService(new FakeRateLimitSecurityEventRepository());

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

    private sealed class FakeRateLimitSecurityEventRepository : IRateLimitSecurityEventRepository
    {
        public RateLimitSecurityEventRecord? Record { get; private set; }

        public Task RecordHitAsync(RateLimitSecurityEventRecord record, CancellationToken cancellationToken)
        {
            Record = record;
            return Task.CompletedTask;
        }
    }
}
