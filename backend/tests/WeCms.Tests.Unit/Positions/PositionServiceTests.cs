using WeCms.Modules.Organization.Positions;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Positions;

public sealed class PositionServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new PositionService(new FakePositionRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(new PositionListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCode()
    {
        var service = new PositionService(new FakePositionRepository { CodeExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreatePositionRequest("dev", "Developer", 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsPositionAssignedToUsers()
    {
        var service = new PositionService(new FakePositionRepository { HasUsers = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    private static PositionRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakePositionRepository : IPositionRepository
    {
        public bool CodeExists { get; init; }
        public bool HasUsers { get; init; }

        public Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<PositionSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<PositionDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<PositionDetailDto?>(new PositionDetailDto(id, "dev", "Developer", 1, "enabled", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptPositionId, CancellationToken cancellationToken) => Task.FromResult(CodeExists);
        public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasUsers);
        public Task<IReadOnlySet<long>> ExistingIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(ids.ToHashSet());
        public Task<long> CreateAsync(PositionCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(PositionUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(PositionAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
