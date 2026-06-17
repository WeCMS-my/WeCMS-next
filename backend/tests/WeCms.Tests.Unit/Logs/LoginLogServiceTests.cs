using WeCms.Modules.System.Logs;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Logs;

public sealed class LoginLogServiceTests
{
    [Fact]
    public async Task ListLoginLogsAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListLoginLogsAsync(new LoginLogListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task ListLoginLogsAsync_RejectsInvalidDateRange()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListLoginLogsAsync(new LoginLogListQuery(From: DateTimeOffset.UnixEpoch.AddDays(1), To: DateTimeOffset.UnixEpoch), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetLoginLogAsync_ReturnsNotFoundWhenMissing()
    {
        var service = new LogService(new FakeLogRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.GetLoginLogAsync(1, CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task ListLoginLogsAsync_PassesFiltersToRepository()
    {
        var repository = new FakeLogRepository();
        var service = new LogService(repository);
        var from = DateTimeOffset.UnixEpoch;
        var to = from.AddDays(1);

        await service.ListLoginLogsAsync(new LoginLogListQuery(2, 30, " admin ", " 192.168.101.199 ", " success ", from, to), CancellationToken.None);

        Assert.Equal(new LoginLogListCriteria(2, 30, "admin", "192.168.101.199", "success", from, to), repository.LastCriteria);
    }

    private sealed class FakeLogRepository : ILogRepository
    {
        public bool Missing { get; init; }
        public LoginLogListCriteria? LastCriteria { get; private set; }

        public Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken)
        {
            LastCriteria = criteria;
            return Task.FromResult(new PagedResult<LoginLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken)
        {
            if (Missing)
            {
                return Task.FromResult<LoginLogDetailDto?>(null);
            }

            return Task.FromResult<LoginLogDetailDto?>(new LoginLogDetailDto(id, "admin", 1, "192.168.101.199", "unit-test", "success", null, DateTimeOffset.UnixEpoch));
        }

        public Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<AuditLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));

        public Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken) => Task.FromResult<AuditLogDetailDto?>(new AuditLogDetailDto(id, 1, "admin", "system", "user", "create", "1", "POST", "/api/v1/system/users", "192.168.101.199", "unit-test", "trace", "success", "created", DateTimeOffset.UnixEpoch));

        public Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<SecurityEventSummaryDto>([], criteria.Page, criteria.PageSize, 0));

        public Task<SecurityEventDetailDto?> GetSecurityEventAsync(long id, CancellationToken cancellationToken) => Task.FromResult<SecurityEventDetailDto?>(new SecurityEventDetailDto(id, "auth.refresh_reuse", 1, "admin", "192.168.101.199", "high", "message", DateTimeOffset.UnixEpoch));
    }
}
