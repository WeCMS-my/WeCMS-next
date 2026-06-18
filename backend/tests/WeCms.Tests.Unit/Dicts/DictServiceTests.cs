using WeCms.Modules.System.Dicts;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Dicts;

public sealed class DictServiceTests
{
    [Fact]
    public async Task DeleteTypeAsync_RejectsSystemType()
    {
        var service = new DictService(new FakeDictRepository { IsSystem = true }, new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteTypeAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task CreateTypeAsync_RejectsDuplicateCode()
    {
        var service = new DictService(new FakeDictRepository { TypeCodeExists = true }, new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateTypeAsync(new CreateDictTypeRequest("content_status", "Content Status", null, 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task CreateValueAsync_RejectsDuplicateValueInType()
    {
        var service = new DictService(new FakeDictRepository { ValueExists = true }, new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateValueAsync("content_status", new CreateDictValueRequest("Published", "published", null, 1, false, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task EnableTypeAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new DictService(repository, unitOfWork);

        await service.EnableTypeAsync(1, Context(), CancellationToken.None);

        Assert.Equal(("type", 1, "enabled"), repository.StatusUpdates.Single());
        Assert.Equal("enable-type", repository.Audits.Single().Action);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task DisableTypeAsync_CanCascadeValues()
    {
        var repository = new FakeDictRepository();
        var service = new DictService(repository, new FakeUnitOfWork());

        await service.DisableTypeAsync(1, new DisableDictTypeRequest(true), Context(), CancellationToken.None);

        Assert.Equal(("type", 1, "disabled"), repository.StatusUpdates.Single());
        Assert.Equal(1, repository.CascadeDisableTypeIds.Single());
        Assert.Equal("disable-type", repository.Audits.Single().Action);
    }

    [Fact]
    public async Task EnableValueAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var service = new DictService(repository, new FakeUnitOfWork());

        await service.EnableValueAsync(7, Context(), CancellationToken.None);

        Assert.Equal(("value", 7, "enabled"), repository.StatusUpdates.Single());
        Assert.Equal("enable-value", repository.Audits.Single().Action);
    }

    [Fact]
    public async Task DisableValueAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var service = new DictService(repository, new FakeUnitOfWork());

        await service.DisableValueAsync(7, Context(), CancellationToken.None);

        Assert.Equal(("value", 7, "disabled"), repository.StatusUpdates.Single());
        Assert.Equal("disable-value", repository.Audits.Single().Action);
    }

    [Fact]
    public async Task DisableValueAsync_RollsBackWhenAuditFails()
    {
        var repository = new FakeDictRepository { ThrowOnAudit = true };
        var unitOfWork = new FakeUnitOfWork();
        var service = new DictService(repository, unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableValueAsync(7, Context(), CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
    }

    private static DictRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakeDictRepository : IDictRepository
    {
        public bool IsSystem { get; init; }
        public bool TypeCodeExists { get; init; }
        public bool ValueExists { get; init; }
        public bool ThrowOnAudit { get; init; }
        public List<(string Resource, long Id, string Status)> StatusUpdates { get; } = [];
        public List<long> CascadeDisableTypeIds { get; } = [];
        public List<DictAuditRecord> Audits { get; } = [];

        public Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<DictTypeSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<DictTypeDetailDto?> GetTypeAsync(long id, CancellationToken cancellationToken) => Task.FromResult<DictTypeDetailDto?>(new DictTypeDetailDto(id, "content_status", "Content Status", null, IsSystem, "enabled", 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<DictTypeDetailDto?> GetTypeByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<DictTypeDetailDto?>(new DictTypeDetailDto(1, code, "Content Status", null, IsSystem, "enabled", 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> TypeCodeExistsAsync(string code, long? exceptTypeId, CancellationToken cancellationToken) => Task.FromResult(TypeCodeExists);
        public Task<bool> TypeHasValuesAsync(long id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<long> CreateTypeAsync(DictTypeCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateTypeAsync(DictTypeUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetTypeStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
        {
            StatusUpdates.Add(("type", id, status));
            return Task.CompletedTask;
        }

        public Task DisableValuesByTypeAsync(long typeId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CascadeDisableTypeIds.Add(typeId);
            return Task.CompletedTask;
        }

        public Task SoftDeleteTypeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DictValueDto>>([]);
        public Task<DictValueDto?> GetValueAsync(long id, CancellationToken cancellationToken) => Task.FromResult<DictValueDto?>(new DictValueDto(id, 1, "content_status", "Published", "published", null, 1, false, "enabled"));
        public Task<bool> ValueExistsAsync(long typeId, string value, long? exceptValueId, CancellationToken cancellationToken) => Task.FromResult(ValueExists);
        public Task<long> CreateValueAsync(DictValueCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(3L);
        public Task UpdateValueAsync(DictValueUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetValueStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
        {
            StatusUpdates.Add(("value", id, status));
            return Task.CompletedTask;
        }

        public Task SoftDeleteValueAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(DictAuditRecord record, CancellationToken cancellationToken)
        {
            Audits.Add(record);
            if (ThrowOnAudit)
            {
                throw new InvalidOperationException("audit failed");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(this));
        }

        private sealed class FakeTransactionContext : ITransactionContext
        {
            private readonly FakeUnitOfWork _unitOfWork;

            public FakeTransactionContext(FakeUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.CommitCalls++;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.RollbackCalls++;
                return Task.CompletedTask;
            }
        }
    }
}
