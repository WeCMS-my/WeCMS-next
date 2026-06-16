using WeCms.Modules.System.Departments;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Departments;

public sealed class DepartmentServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsDepartmentWithChildren()
    {
        var service = new DepartmentService(new FakeDepartmentRepository { HasChildren = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsDepartmentAssignedToUsers()
    {
        var service = new DepartmentService(new FakeDepartmentRepository { HasUsers = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDescendantParent()
    {
        var service = new DepartmentService(new FakeDepartmentRepository { ParentIsDescendant = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(1, new UpdateDepartmentRequest(2, "Dept", 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    private static DepartmentRequestContext Context()
    {
        return new DepartmentRequestContext(1, "admin", "127.0.0.1", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public bool HasChildren { get; init; }
        public bool HasUsers { get; init; }
        public bool ParentIsDescendant { get; init; }

        public Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DepartmentSummaryDto>>([]);
        public Task<DepartmentDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<DepartmentDetailDto?>(new DepartmentDetailDto(id, null, "root", "Root", 1, "enabled", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptDepartmentId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasChildren);
        public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasUsers);
        public Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken) => Task.FromResult(ParentIsDescendant);
        public Task<long> CreateAsync(DepartmentCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(DepartmentUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(DepartmentAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
