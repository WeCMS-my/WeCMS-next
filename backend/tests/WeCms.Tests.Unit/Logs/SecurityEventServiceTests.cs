using WeCms.Modules.Security.Events;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Logs;

public sealed class SecurityEventServiceTests
{
    [Fact]
    public async Task ListSecurityEventsAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new SecurityEventService(new FakeSecurityEventRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListSecurityEventsAsync(new SecurityEventListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task ListSecurityEventsAsync_RejectsInvalidDateRange()
    {
        var service = new SecurityEventService(new FakeSecurityEventRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListSecurityEventsAsync(new SecurityEventListQuery(From: DateTimeOffset.UnixEpoch.AddDays(1), To: DateTimeOffset.UnixEpoch), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetSecurityEventAsync_ReturnsNotFoundWhenMissing()
    {
        var service = new SecurityEventService(new FakeSecurityEventRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.GetSecurityEventAsync(1, CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task ListSecurityEventsAsync_PassesFiltersToRepository()
    {
        var repository = new FakeSecurityEventRepository();
        var service = new SecurityEventService(repository);
        var from = DateTimeOffset.UnixEpoch;
        var to = from.AddDays(1);

        await service.ListSecurityEventsAsync(new SecurityEventListQuery(2, 30, " auth.refresh_reuse ", " high ", " admin ", " 192.168.101.199 ", from, to), CancellationToken.None);

        Assert.Equal(new SecurityEventListCriteria(2, 30, "auth.refresh_reuse", "high", "admin", "192.168.101.199", from, to), repository.LastSecurityCriteria);
    }

    private sealed class FakeSecurityEventRepository : ISecurityEventRepository
    {
        public bool Missing { get; init; }
        public SecurityEventListCriteria? LastSecurityCriteria { get; private set; }
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

            return Task.FromResult<SecurityEventDetailDto?>(new SecurityEventDetailDto(id, "auth.refresh_reuse", 1, "admin", "192.168.101.199", "high", "auth", "trace", "message", DateTimeOffset.UnixEpoch));
        }
    }
}
