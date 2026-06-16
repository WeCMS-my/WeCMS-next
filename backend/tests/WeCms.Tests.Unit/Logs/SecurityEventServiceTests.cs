using WeCms.Modules.System.Logs;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Logs;

public sealed class SecurityEventServiceTests
{
    [Fact]
    public async Task ListSecurityEventsAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListSecurityEventsAsync(new SecurityEventListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task ListSecurityEventsAsync_RejectsInvalidDateRange()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListSecurityEventsAsync(new SecurityEventListQuery(From: DateTimeOffset.UnixEpoch.AddDays(1), To: DateTimeOffset.UnixEpoch), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetSecurityEventAsync_ReturnsNotFoundWhenMissing()
    {
        var service = new LogService(new FakeLogRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.GetSecurityEventAsync(1, CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task ListSecurityEventsAsync_PassesFiltersToRepository()
    {
        var repository = new FakeLogRepository();
        var service = new LogService(repository);
        var from = DateTimeOffset.UnixEpoch;
        var to = from.AddDays(1);

        await service.ListSecurityEventsAsync(new SecurityEventListQuery(2, 30, " auth.refresh_reuse ", " high ", " admin ", " 127.0.0.1 ", from, to), CancellationToken.None);

        Assert.Equal(new SecurityEventListCriteria(2, 30, "auth.refresh_reuse", "high", "admin", "127.0.0.1", from, to), repository.LastSecurityCriteria);
    }

    private sealed class FakeLogRepository : ILogRepository
    {
        public bool Missing { get; init; }
        public SecurityEventListCriteria? LastSecurityCriteria { get; private set; }

        public Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<LoginLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken) => Task.FromResult<LoginLogDetailDto?>(new LoginLogDetailDto(id, "admin", 1, "127.0.0.1", "unit-test", "success", null, DateTimeOffset.UnixEpoch));
        public Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<AuditLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken) => Task.FromResult<AuditLogDetailDto?>(new AuditLogDetailDto(id, 1, "admin", "system", "user", "create", "1", "POST", "/api/v1/system/users", "127.0.0.1", "unit-test", "trace", "success", "created", DateTimeOffset.UnixEpoch));

        public Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListCriteria criteria, CancellationToken cancellationToken)
        {
            LastSecurityCriteria = criteria;
            return Task.FromResult(new PagedResult<SecurityEventSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<SecurityEventDetailDto?> GetSecurityEventAsync(long id, CancellationToken cancellationToken)
        {
            if (Missing)
            {
                return Task.FromResult<SecurityEventDetailDto?>(null);
            }

            return Task.FromResult<SecurityEventDetailDto?>(new SecurityEventDetailDto(id, "auth.refresh_reuse", 1, "admin", "127.0.0.1", "high", "message", DateTimeOffset.UnixEpoch));
        }
    }
}
