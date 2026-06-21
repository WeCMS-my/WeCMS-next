using WeCms.Modules.Organization;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.Positions;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Organization;

public sealed class OrganizationLookupServiceTests
{
    [Fact]
    public async Task DepartmentExistsAsync_DelegatesToDepartmentRepository()
    {
        var departmentRepository = new FakeDepartmentRepository { Exists = true };
        var service = new OrganizationLookupService(departmentRepository, new FakePositionRepository());

        var exists = await service.DepartmentExistsAsync(7, CancellationToken.None);

        Assert.True(exists);
        Assert.Equal(7, departmentRepository.LastCheckedId);
    }

    [Fact]
    public async Task ExistingPositionIdsAsync_DelegatesToPositionRepository()
    {
        var positionRepository = new FakePositionRepository { ExistingIds = new HashSet<long> { 3 } };
        var service = new OrganizationLookupService(new FakeDepartmentRepository(), positionRepository);

        var existing = await service.ExistingPositionIdsAsync([3, 4], CancellationToken.None);

        Assert.Equal(new long[] { 3 }, existing.Order());
        Assert.Equal(new long[] { 3, 4 }, positionRepository.LastCheckedIds);
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public bool Exists { get; init; }
        public long LastCheckedId { get; private set; }

        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
        {
            LastCheckedId = id;
            return Task.FromResult(Exists);
        }

        public Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DepartmentDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> CodeExistsAsync(string code, long? exceptDepartmentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> CreateAsync(DepartmentCreateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateAsync(DepartmentUpdateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RecordAuditAsync(DepartmentAuditRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakePositionRepository : IPositionRepository
    {
        public IReadOnlySet<long> ExistingIds { get; init; } = new HashSet<long>();
        public IReadOnlyList<long> LastCheckedIds { get; private set; } = [];

        public Task<IReadOnlySet<long>> ExistingIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken)
        {
            LastCheckedIds = ids.ToArray();
            return Task.FromResult(ExistingIds);
        }

        public Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListCriteria criteria, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PositionDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> CodeExistsAsync(string code, long? exceptPositionId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> CreateAsync(PositionCreateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateAsync(PositionUpdateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RecordAuditAsync(PositionAuditRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
