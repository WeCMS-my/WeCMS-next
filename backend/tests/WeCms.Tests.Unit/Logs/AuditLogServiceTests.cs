using WeCms.Modules.Audit.Logs;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Logs;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task ListAuditLogsAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAuditLogsAsync(new AuditLogListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task ListAuditLogsAsync_RejectsInvalidDateRange()
    {
        var service = new LogService(new FakeLogRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAuditLogsAsync(new AuditLogListQuery(From: DateTimeOffset.UnixEpoch.AddDays(1), To: DateTimeOffset.UnixEpoch), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetAuditLogAsync_ReturnsNotFoundWhenMissing()
    {
        var service = new LogService(new FakeLogRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.GetAuditLogAsync(1, CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task ListAuditLogsAsync_PassesFiltersToRepository()
    {
        var repository = new FakeLogRepository();
        var service = new LogService(repository);
        var from = DateTimeOffset.UnixEpoch;
        var to = from.AddDays(1);

        await service.ListAuditLogsAsync(new AuditLogListQuery(2, 30, " admin ", " system ", " user ", " create ", " success ", from, to), CancellationToken.None);

        Assert.Equal(new AuditLogListCriteria(2, 30, "admin", "system", "user", "create", "success", from, to), repository.LastAuditCriteria);
    }

    private sealed class FakeLogRepository : ILogRepository
    {
        public bool Missing { get; init; }
        public AuditLogListCriteria? LastAuditCriteria { get; private set; }

        public Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<LoginLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken) => Task.FromResult<LoginLogDetailDto?>(new LoginLogDetailDto(id, "admin", 1, "192.168.101.199", "unit-test", "success", null, DateTimeOffset.UnixEpoch));

        public Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken)
        {
            LastAuditCriteria = criteria;
            return Task.FromResult(new PagedResult<AuditLogSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken)
        {
            if (Missing)
            {
                return Task.FromResult<AuditLogDetailDto?>(null);
            }

            return Task.FromResult<AuditLogDetailDto?>(new AuditLogDetailDto(id, 1, "admin", "system", "user", "create", "1", "POST", "/api/v1/system/users", "192.168.101.199", "unit-test", "trace", "success", "created", DateTimeOffset.UnixEpoch));
        }
    }
}
