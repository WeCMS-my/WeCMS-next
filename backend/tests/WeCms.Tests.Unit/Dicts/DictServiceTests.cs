using WeCms.Modules.Configuration;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.Events;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Dicts;

public sealed class DictServiceTests
{
    [Fact]
    public async Task DeleteTypeAsync_RejectsSystemType()
    {
        var service = CreateService(new FakeDictRepository { IsSystem = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteTypeAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task CreateTypeAsync_RejectsDuplicateCode()
    {
        var service = CreateService(new FakeDictRepository { TypeCodeExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateTypeAsync(new CreateDictTypeRequest("content_status", "Content Status", null, 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task CreateValueAsync_RejectsDuplicateValueInType()
    {
        var service = CreateService(new FakeDictRepository { ValueExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateValueAsync("content_status", new CreateDictValueRequest("Published", "published", null, 1, false, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task EnableTypeAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var unitOfWork = new FakeUnitOfWork();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, unitOfWork, cacheInvalidator);

        await service.EnableTypeAsync(1, Context(), CancellationToken.None);

        Assert.Equal(("type", 1, "enabled"), repository.StatusUpdates.Single());
        Assert.Equal("enable-type", repository.Audits.Single().Action);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task DisableTypeAsync_CanCascadeValues()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.DisableTypeAsync(1, new DisableDictTypeRequest(true), Context(), CancellationToken.None);

        Assert.Equal(("type", 1, "disabled"), repository.StatusUpdates.Single());
        Assert.Equal(1, repository.CascadeDisableTypeIds.Single());
        Assert.Equal("disable-type", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task EnableValueAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.EnableValueAsync(7, Context(), CancellationToken.None);

        Assert.Equal(("value", 7, "enabled"), repository.StatusUpdates.Single());
        Assert.Equal("enable-value", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task DisableValueAsync_UpdatesStatusAndAudits()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.DisableValueAsync(7, Context(), CancellationToken.None);

        Assert.Equal(("value", 7, "disabled"), repository.StatusUpdates.Single());
        Assert.Equal("disable-value", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task DisableValueAsync_RollsBackWhenAuditFails()
    {
        var repository = new FakeDictRepository { ThrowOnAudit = true };
        var unitOfWork = new FakeUnitOfWork();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, unitOfWork, cacheInvalidator);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableValueAsync(7, Context(), CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(0, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task CreateTypeAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        var result = await service.CreateTypeAsync(new CreateDictTypeRequest("content_status", "Content Status", null, 1, "enabled"), Context(), CancellationToken.None);

        Assert.Equal(2, result.Id);
        Assert.Equal("create-type", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task CreateTypeAsync_WritesDictChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new WeCms.Tests.Unit.RecordingOutboxWriter(operations);
        var service = CreateService(new FakeDictRepository(), new FakeUnitOfWork(operations), outboxWriter: outbox);

        await service.CreateTypeAsync(new CreateDictTypeRequest("content_status", "Content Status", null, 1, "enabled"), Context(), CancellationToken.None);

        var evt = Assert.IsType<DictChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(DictChangedEvent.EventType, evt.Type);
        Assert.Equal("dict-type", evt.Resource);
        Assert.Equal(2, evt.TargetId);
        AssertWriteWasInsideTransaction(operations, DictChangedEvent.EventType);
    }

    [Fact]
    public async Task DictChangedEvent_EvictsDictCache()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var handler = new DictChangedCacheHandler(cacheInvalidator);

        await handler.HandleAsync(new DictChangedEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), Context().Now, "trace", null, "dict-type", 2), CancellationToken.None);

        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task UpdateTypeAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        var result = await service.UpdateTypeAsync(1, new UpdateDictTypeRequest("Content Status", null, 2, "enabled"), Context(), CancellationToken.None);

        Assert.Equal(1, result.Id);
        Assert.Equal("update-type", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task DeleteTypeAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.DeleteTypeAsync(1, Context(), CancellationToken.None);

        Assert.Equal("delete-type", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task CreateValueAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        var result = await service.CreateValueAsync("content_status", new CreateDictValueRequest("Published", "published", null, 1, false, "enabled"), Context(), CancellationToken.None);

        Assert.Equal(3, result.Id);
        Assert.Equal("create-value", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task UpdateValueAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        var result = await service.UpdateValueAsync(7, new UpdateDictValueRequest("Draft", "draft", null, 2, false, "enabled"), Context(), CancellationToken.None);

        Assert.Equal(7, result.Id);
        Assert.Equal("update-value", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    [Fact]
    public async Task DeleteValueAsync_InvalidatesDictCache()
    {
        var repository = new FakeDictRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.DeleteValueAsync(7, Context(), CancellationToken.None);

        Assert.Equal("delete-value", repository.Audits.Single().Action);
        Assert.Equal(1, cacheInvalidator.DictInvalidations);
    }

    private static DictRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private static DictService CreateService(
        FakeDictRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        FakeConfigurationCacheInvalidator? cacheInvalidator = null,
        WeCms.Tests.Unit.RecordingOutboxWriter? outboxWriter = null)
    {
        return new DictService(
            repository,
            unitOfWork ?? new FakeUnitOfWork(),
            cacheInvalidator ?? new FakeConfigurationCacheInvalidator(),
            outboxWriter ?? new WeCms.Tests.Unit.RecordingOutboxWriter(),
            new WeCms.Tests.Unit.FixedTestIdGenerator());
    }

    private static void AssertWriteWasInsideTransaction(IReadOnlyList<string> operations, string eventType)
    {
        var orderedOperations = operations.ToList();
        var begin = orderedOperations.IndexOf("begin");
        var outbox = orderedOperations.IndexOf($"outbox:{eventType}");
        var commit = orderedOperations.IndexOf("commit");

        Assert.True(begin >= 0, string.Join(", ", operations));
        Assert.True(outbox > begin, string.Join(", ", operations));
        Assert.True(commit > outbox, string.Join(", ", operations));
    }

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
        private readonly List<string>? _operations;
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public FakeUnitOfWork(List<string>? operations = null)
        {
            _operations = operations;
        }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _operations?.Add("begin");
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
                _unitOfWork._operations?.Add("commit");
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.RollbackCalls++;
                _unitOfWork._operations?.Add("rollback");
                return Task.CompletedTask;
            }
        }
    }

    private sealed class FakeConfigurationCacheInvalidator : IConfigurationCacheInvalidator
    {
        public int DictInvalidations { get; private set; }

        public Task InvalidateSettingsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task InvalidateDictsAsync(CancellationToken cancellationToken)
        {
            DictInvalidations++;
            return Task.CompletedTask;
        }

        public Task InvalidateI18nAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
