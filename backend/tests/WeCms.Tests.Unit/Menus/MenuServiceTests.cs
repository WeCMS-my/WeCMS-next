using WeCms.Modules.System.Menus;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Menus;

public sealed class MenuServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsBuiltinMenu()
    {
        var service = new MenuService(new FakeMenuRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsMenuWithChildren()
    {
        var service = new MenuService(new FakeMenuRepository { IsBuiltin = false, HasChildren = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDescendantParent()
    {
        var service = new MenuService(new FakeMenuRepository { IsBuiltin = false, ParentIsDescendant = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(1, new UpdateMenuRequest(2, "Menu", "/new", "component", "Title", null, null, 1, false, false, null, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    private static MenuRequestContext Context()
    {
        return new MenuRequestContext(1, "admin", "127.0.0.1", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakeMenuRepository : IMenuRepository
    {
        public bool IsBuiltin { get; init; } = true;
        public bool HasChildren { get; init; }
        public bool ParentIsDescendant { get; init; }

        public Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MenuSummaryDto>>([]);
        public Task<MenuDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<MenuDetailDto?>(new MenuDetailDto(id, null, "catalog", "sys.system", "/system", "layout.base", "System", null, null, 1, false, false, null, null, "enabled", IsBuiltin, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptMenuId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasChildren);
        public Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken) => Task.FromResult(ParentIsDescendant);
        public Task<long> CreateAsync(MenuCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(MenuUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(MenuAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
