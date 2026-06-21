using WeCms.Modules.Security;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Auth;

public sealed class LoginFailureLimiterTests
{
    [Fact]
    public async Task RecordFailureAsync_ThresholdWritesSecurityEventAndBlocks()
    {
        var repository = new FakeLoginFailureCounterRepository
        {
            UsernameCounter = new LoginFailureCounterRecord("username", "admin", 3),
            IpCounter = new LoginFailureCounterRecord("ip", "192.168.1.10", 1)
        };
        var limiter = CreateLimiter(repository, new FakeSecurityBanService());

        var decision = await limiter.RecordFailureAsync(Context(userId: 7), CancellationToken.None);

        Assert.True(decision.IsBlocked);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("auth.login_rate_limited", repository.LastSecurityEventType);
        Assert.Equal(0, repository.CreatedBanCount);
    }

    [Fact]
    public async Task RecordFailureAsync_BanThresholdCreatesTemporaryUserAndIpBans()
    {
        var repository = new FakeLoginFailureCounterRepository
        {
            UsernameCounter = new LoginFailureCounterRecord("username", "admin", 5),
            IpCounter = new LoginFailureCounterRecord("ip", "192.168.1.10", 5)
        };
        var securityBanService = new FakeSecurityBanService();
        var limiter = CreateLimiter(repository, securityBanService);

        var decision = await limiter.RecordFailureAsync(Context(userId: 7), CancellationToken.None);

        Assert.True(decision.IsBlocked);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal(2, securityBanService.CreateTemporaryCalls);
        Assert.Contains(securityBanService.CreatedTargets, target => target == "7");
        Assert.Contains(securityBanService.CreatedTargets, target => target == "192.168.1.10");
    }

    [Fact]
    public async Task ResetAsync_ClearsUsernameAndIpCounters()
    {
        var repository = new FakeLoginFailureCounterRepository();
        var limiter = CreateLimiter(repository, new FakeSecurityBanService());

        await limiter.ResetAsync(" admin ", "192.168.1.10", CancellationToken.None);

        Assert.Equal(["username:admin", "ip:192.168.1.10"], repository.ResetKeys);
    }

    private static LoginFailureLimiter CreateLimiter(
        ILoginFailureCounterRepository repository,
        IIdentitySecurityBanService securityBanService)
    {
        return new LoginFailureLimiter(
            repository,
            securityBanService,
            new FakeSecurityAlertService(),
            new LoginFailurePolicyOptions(
                Enabled: true,
                Window: TimeSpan.FromMinutes(10),
                UsernameThreshold: 3,
                IpThreshold: 4,
                BanThreshold: 5,
                BanDuration: TimeSpan.FromMinutes(15)));
    }

    private static LoginFailureContext Context(long? userId)
    {
        return new LoginFailureContext(
            "admin",
            userId,
            "192.168.1.10",
            "unit-test",
            new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeLoginFailureCounterRepository : ILoginFailureCounterRepository
    {
        public LoginFailureCounterRecord UsernameCounter { get; init; } = new("username", "admin", 1);

        public LoginFailureCounterRecord IpCounter { get; init; } = new("ip", "192.168.1.10", 1);

        public int SecurityEventCount { get; private set; }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public int CreatedBanCount { get; private set; }

        public List<string> ResetKeys { get; } = [];

        public Task<LoginFailureCounterRecord> IncrementAsync(LoginFailureCounterIncrement record, CancellationToken cancellationToken)
        {
            return Task.FromResult(record.Scope == "username" ? UsernameCounter : IpCounter);
        }

        public Task ResetAsync(string scope, string target, CancellationToken cancellationToken)
        {
            ResetKeys.Add($"{scope}:{target}");
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityBanService : IIdentitySecurityBanService
    {
        public int CreateTemporaryCalls { get; private set; }

        public List<string> CreatedTargets { get; } = [];

        public Task CreateTemporaryAsync(IdentitySecurityBanCreateRecord record, CancellationToken cancellationToken)
        {
            CreateTemporaryCalls++;
            CreatedTargets.Add(record.Target);
            return Task.CompletedTask;
        }

        public Task<bool> HasActiveAsync(string banType, string target, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FakeSecurityAlertService : IIdentitySecurityAlertService
    {
        public Task PublishIfRequiredAsync(
            string eventType,
            string severity,
            string message,
            string traceId,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
