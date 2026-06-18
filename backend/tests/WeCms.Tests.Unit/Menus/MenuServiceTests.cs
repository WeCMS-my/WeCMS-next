using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Menus;

public sealed class MenuServiceTests
{
    [Fact]
    public async Task DeleteAsync_RejectsBuiltinMenu()
    {
        var service = CreateService(new FakeMenuRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsMenuWithChildren()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = false, HasChildren = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task EnableAsync_RejectsBuiltinMenu()
    {
        var service = CreateService(new FakeMenuRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.EnableAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("System built-in menus cannot be enabled.", exception.Message);
    }

    [Fact]
    public async Task DisableAsync_RejectsBuiltinMenu()
    {
        var service = CreateService(new FakeMenuRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("System built-in menus cannot be disabled.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDescendantParent()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = false, ParentIsDescendant = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(1, new UpdateMenuRequest(2, "Menu", "/new", "component", "Title", null, null, 1, false, false, null, null, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task SortAsync_RejectsTooManyItems()
    {
        var service = CreateService(new FakeMenuRepository());
        var items = Enumerable.Range(1, 201)
            .Select(index => new SortMenuItemRequest(index, null, index))
            .ToArray();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.SortAsync(new SortMenusRequest(items), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task SortAsync_RejectsBuiltinMenu()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.SortAsync(new SortMenusRequest([new SortMenuItemRequest(1, null, 10)]), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task SortAsync_RejectsDescendantParent()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = false, ParentIsDescendant = true });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.SortAsync(new SortMenusRequest([new SortMenuItemRequest(1, 2, 10)]), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task SortAsync_RejectsUnknownMenu()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = false, MissingIds = [2] });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.SortAsync(new SortMenusRequest([new SortMenuItemRequest(2, null, 10)]), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task SortAsync_RejectsRequestedCycle()
    {
        var service = CreateService(new FakeMenuRepository { IsBuiltin = false });

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.SortAsync(
                new SortMenusRequest(
                [
                    new SortMenuItemRequest(1, 2, 10),
                    new SortMenuItemRequest(2, 1, 20)
                ]),
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task SortAsync_UpdatesBatchAndAuditsInTransaction()
    {
        var repository = new FakeMenuRepository { IsBuiltin = false };
        var unitOfWork = new FakeUnitOfWork();
        var permissionVersionService = new FakePermissionVersionService();
        var service = CreateService(repository, unitOfWork, permissionVersionService);

        await service.SortAsync(
            new SortMenusRequest(
            [
                new SortMenuItemRequest(1, null, 10),
                new SortMenuItemRequest(2, 1, 20)
            ]),
            Context(),
            CancellationToken.None);

        Assert.Equal(2, repository.Sorted.Count);
        var audit = Assert.Single(repository.Audits);
        Assert.Equal("sort", audit.Action);
        Assert.True(unitOfWork.TransactionCommitted);
        Assert.False(unitOfWork.TransactionRolledBack);
        Assert.Equal([1, 2], permissionVersionService.BumpedMenuIds);
    }

    [Fact]
    public async Task SortAsync_NormalizesRootZeroAndRollsBackWhenRepositoryFails()
    {
        var repository = new FakeMenuRepository { IsBuiltin = false, ThrowOnSort = true };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SortAsync(new SortMenusRequest([new SortMenuItemRequest(1, 0, 10)]), Context(), CancellationToken.None));

        var sorted = Assert.Single(repository.Sorted);
        Assert.Null(sorted.ParentId);
        Assert.False(unitOfWork.TransactionCommitted);
        Assert.True(unitOfWork.TransactionRolledBack);
    }

    [Fact]
    public async Task SortAsync_AllowsCrossParentMove()
    {
        var repository = new FakeMenuRepository { IsBuiltin = false };
        var service = CreateService(repository);

        await service.SortAsync(new SortMenusRequest([new SortMenuItemRequest(2, 1, 10)]), Context(), CancellationToken.None);

        var sorted = Assert.Single(repository.Sorted);
        Assert.Equal(2, sorted.Id);
        Assert.Equal(1, sorted.ParentId);
    }

    private static MenuRequestContext Context()
    {
        return new MenuRequestContext(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private static MenuService CreateService(
        FakeMenuRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        FakePermissionVersionService? permissionVersionService = null)
    {
        return new MenuService(repository, unitOfWork ?? new FakeUnitOfWork(), permissionVersionService ?? new FakePermissionVersionService());
    }

    private sealed class FakeMenuRepository : IMenuRepository
    {
        public bool IsBuiltin { get; init; } = true;
        public bool HasChildren { get; init; }
        public bool ParentIsDescendant { get; init; }
        public HashSet<long> MissingIds { get; init; } = [];
        public bool ThrowOnSort { get; init; }
        public List<MenuSortRecord> Sorted { get; } = [];
        public List<MenuAuditRecord> Audits { get; } = [];

        public Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MenuSummaryDto>>([]);
        public Task<MenuDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<MenuDetailDto?>(MissingIds.Contains(id) ? null : new MenuDetailDto(id, null, "catalog", "sys.system", "/system", "layout.base", "System", null, null, 1, false, false, null, null, "enabled", IsBuiltin, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        }
        public Task<bool> CodeExistsAsync(string code, long? exceptMenuId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasChildren);
        public Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken) => Task.FromResult(ParentIsDescendant);
        public Task<long> CreateAsync(MenuCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(MenuUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SortAsync(IReadOnlyList<MenuSortRecord> records, CancellationToken cancellationToken)
        {
            Sorted.AddRange(records);
            if (ThrowOnSort)
            {
                throw new InvalidOperationException("sort failed");
            }

            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(MenuAuditRecord record, CancellationToken cancellationToken)
        {
            Audits.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly FakeTransactionContext _transaction;

        public FakeUnitOfWork()
        {
            _transaction = new FakeTransactionContext(this);
        }

        public bool TransactionCommitted { get; private set; }
        public bool TransactionRolledBack { get; private set; }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(_transaction);
        }

        private sealed class FakeTransactionContext : ITransactionContext
        {
            private readonly FakeUnitOfWork _unitOfWork;

            public FakeTransactionContext(FakeUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.TransactionCommitted = true;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.TransactionRolledBack = true;
                return Task.CompletedTask;
            }
        }
    }

    private sealed class FakePermissionVersionService : IPermissionVersionService
    {
        public IReadOnlyList<long> BumpedMenuIds => _bumpedMenuIds;
        private readonly List<long> _bumpedMenuIds = [];

        public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _bumpedMenuIds.Add(menuId);
            return Task.CompletedTask;
        }

        public Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _bumpedMenuIds.AddRange(menuIds);
            return Task.CompletedTask;
        }
    }
}
