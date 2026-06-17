using WeCms.Modules.System.Dicts;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Dicts;

public sealed class DictServiceTests
{
    [Fact]
    public async Task DeleteTypeAsync_RejectsSystemType()
    {
        var service = new DictService(new FakeDictRepository { IsSystem = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteTypeAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task CreateTypeAsync_RejectsDuplicateCode()
    {
        var service = new DictService(new FakeDictRepository { TypeCodeExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateTypeAsync(new CreateDictTypeRequest("content_status", "Content Status", null, 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task CreateValueAsync_RejectsDuplicateValueInType()
    {
        var service = new DictService(new FakeDictRepository { ValueExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateValueAsync("content_status", new CreateDictValueRequest("Published", "published", null, 1, false, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    private static DictRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakeDictRepository : IDictRepository
    {
        public bool IsSystem { get; init; }
        public bool TypeCodeExists { get; init; }
        public bool ValueExists { get; init; }

        public Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<DictTypeSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<DictTypeDetailDto?> GetTypeAsync(long id, CancellationToken cancellationToken) => Task.FromResult<DictTypeDetailDto?>(new DictTypeDetailDto(id, "content_status", "Content Status", null, IsSystem, "enabled", 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<DictTypeDetailDto?> GetTypeByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<DictTypeDetailDto?>(new DictTypeDetailDto(1, code, "Content Status", null, IsSystem, "enabled", 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> TypeCodeExistsAsync(string code, long? exceptTypeId, CancellationToken cancellationToken) => Task.FromResult(TypeCodeExists);
        public Task<bool> TypeHasValuesAsync(long id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<long> CreateTypeAsync(DictTypeCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateTypeAsync(DictTypeUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteTypeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DictValueDto>>([]);
        public Task<DictValueDto?> GetValueAsync(long id, CancellationToken cancellationToken) => Task.FromResult<DictValueDto?>(new DictValueDto(id, 1, "content_status", "Published", "published", null, 1, false, "enabled"));
        public Task<bool> ValueExistsAsync(long typeId, string value, long? exceptValueId, CancellationToken cancellationToken) => Task.FromResult(ValueExists);
        public Task<long> CreateValueAsync(DictValueCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(3L);
        public Task UpdateValueAsync(DictValueUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteValueAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(DictAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
