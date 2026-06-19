using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Security;

public sealed class SecurityBanServiceTests
{
    [Fact]
    public async Task FindActiveAsync_RejectsUnknownBanType()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.FindActiveAsync("device", "device-1", Now, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task FindActiveAsync_NormalizesTargetAndDelegatesToRepository()
    {
        var repository = new FakeSecurityBanRepository
        {
            ActiveBan = new SecurityBanRecord(7, SecurityBanTypes.Ip, "192.168.1.10", "bruteforce", "warning", "login", Now.AddMinutes(10), null)
        };
        var service = CreateService(repository);

        var ban = await service.FindActiveAsync(SecurityBanTypes.Ip, " 192.168.1.10 ", Now, CancellationToken.None);

        Assert.NotNull(ban);
        Assert.Equal(SecurityBanTypes.Ip, repository.LastBanType);
        Assert.Equal("192.168.1.10", repository.LastTarget);
    }

    [Fact]
    public async Task RecordHitAsync_WritesSecurityEvent()
    {
        var repository = new FakeSecurityBanRepository();
        var alertService = new FakeSecurityAlertService();
        var service = CreateService(repository, alertService);

        await service.RecordHitAsync(
            new SecurityBanRecord(7, SecurityBanTypes.User, "42", "admin reset", "critical", "manual", null, null),
            new SecurityBanHitContext(42, "admin", "192.168.1.10", "unit-test", "trace-ban", Now),
            CancellationToken.None);

        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("security.ban_hit", repository.LastSecurityEventType);
        Assert.Equal("critical", repository.LastSecurityEventSeverity);
        Assert.Equal(1, alertService.Count);
    }

    [Fact]
    public async Task ListAsync_RejectsOversizedPageSize()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new SecurityBanListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UnbanAsync_RequiresReason()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest(" "), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UnbanAsync_RejectsAlreadyRevokedBan()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.Ip, "192.168.1.10", "bruteforce", "warning", "login", null, 1, Now, "fixed", Now, Now, null, null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal(0, repository.RevokeCalls);
    }

    [Fact]
    public async Task UnbanAsync_WritesAuditAndSecurityEvent()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.User, "42", "manual", "critical", "admin", null, null, null, null, Now, Now, null, null)
        };
        var service = CreateService(repository);

        var response = await service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None);

        Assert.Equal(7, response.Id);
        Assert.Equal(1, repository.RevokeCalls);
        Assert.Equal(1, repository.AuditCount);
        Assert.Equal("unban", repository.LastAuditAction);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("security.ban_unbanned", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task UnbanAsync_RejectsCriticalSelfUserBanForNonSuperAdmin()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.User, "9", "manual", "critical", "admin", null, null, null, null, Now, Now, null, null),
            IsSuperAdmin = false
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.Forbidden, exception.Code);
        Assert.Equal(0, repository.RevokeCalls);
    }

    [Fact]
    public async Task BatchUnbanAsync_RejectsTooManyIds()
    {
        var service = CreateService(new FakeSecurityBanRepository());
        var ids = Enumerable.Range(1, 51).Select(static value => (long)value).ToArray();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.BatchUnbanAsync(new BatchUnbanSecurityBansRequest(ids, "reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private static readonly DateTimeOffset Now = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    private static readonly SecurityBanRequestContext Context = new(9, "operator", "127.0.0.1", "unit-test", "trace-security", Now);

    private static SecurityBanService CreateService(
        ISecurityBanRepository repository,
        FakeSecurityAlertService? alertService = null)
    {
        return new SecurityBanService(repository, alertService ?? new FakeSecurityAlertService());
    }

    private sealed class FakeSecurityBanRepository : ISecurityBanRepository
    {
        public SecurityBanRecord? ActiveBan { get; init; }

        public SecurityBanDetailDto? BanDetail { get; init; }

        public string LastBanType { get; private set; } = string.Empty;

        public string LastTarget { get; private set; } = string.Empty;

        public int SecurityEventCount { get; private set; }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public string LastSecurityEventSeverity { get; private set; } = string.Empty;

        public int RevokeCalls { get; private set; }

        public int AuditCount { get; private set; }

        public string LastAuditAction { get; private set; } = string.Empty;

        public bool IsSuperAdmin { get; init; } = true;

        public Task<SecurityBanRecord?> FindActiveAsync(string banType, string target, DateTimeOffset now, CancellationToken cancellationToken)
        {
            LastBanType = banType;
            LastTarget = target;
            return Task.FromResult(ActiveBan);
        }

        public Task<SecurityStatusDto> GetStatusAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SecurityStatusDto(0, 0, 0, 0, now));
        }

        public Task<PagedResult<SecurityBanSummaryDto>> ListAsync(SecurityBanListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<SecurityBanSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<SecurityBanDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult(BanDetail);
        }

        public Task RevokeAsync(SecurityBanRevokeRecord record, CancellationToken cancellationToken)
        {
            RevokeCalls++;
            return Task.CompletedTask;
        }

        public Task<long> CreateAsync(CreateSecurityBanRecord record, CancellationToken cancellationToken)
        {
            return Task.FromResult(11L);
        }

        public Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsSuperAdmin);
        }

        public Task RecordAuditAsync(SecurityBanAuditRecord record, CancellationToken cancellationToken)
        {
            AuditCount++;
            LastAuditAction = record.Action;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityBanSecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            LastSecurityEventSeverity = record.Severity;
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
}
