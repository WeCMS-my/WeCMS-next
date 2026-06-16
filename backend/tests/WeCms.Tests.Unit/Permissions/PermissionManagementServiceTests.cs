using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Permissions;

public sealed class PermissionManagementServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsBuiltinPermission()
    {
        var service = new PermissionManagementService(new FakePermissionRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCode()
    {
        var service = new PermissionManagementService(new FakePermissionRepository { CodeExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CreateAsync(new CreatePermissionRequest("sys:test:list", "Test list", "system", null), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesRoleBoundCustomPermission()
    {
        var repository = new FakePermissionRepository { IsBuiltin = false, RoleBound = true };
        var service = new PermissionManagementService(repository);

        await service.DeleteAsync(1, Context(), CancellationToken.None);

        Assert.True(repository.SoftDeleted);
    }

    private static PermissionRequestContext Context()
    {
        return new PermissionRequestContext(1, "admin", "127.0.0.1", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakePermissionRepository : IPermissionRepository
    {
        public bool CodeExists { get; init; }
        public bool IsBuiltin { get; init; } = true;
        public bool RoleBound { get; init; }
        public bool SoftDeleted { get; private set; }

        public Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<PermissionUserRecord?>(new PermissionUserRecord(userId, "enabled"));
        public Task<bool> UserHasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<PermissionSummaryDto>> ListManagementAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PermissionSummaryDto>>([]);
        public Task<PermissionDetailDto?> GetManagementAsync(long id, CancellationToken cancellationToken) => Task.FromResult<PermissionDetailDto?>(new PermissionDetailDto(id, "sys:test:list", "Test list", "system", null, "enabled", IsBuiltin, RoleBound, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptPermissionId, CancellationToken cancellationToken) => Task.FromResult(CodeExists);
        public Task<long> CreateManagementAsync(PermissionCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateManagementAsync(PermissionUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteManagementAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
        {
            SoftDeleted = true;
            return Task.CompletedTask;
        }

        public Task SetManagementStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordManagementAuditAsync(PermissionAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
