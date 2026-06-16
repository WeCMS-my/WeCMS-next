using WeCms.Shared;

namespace WeCms.Modules.System.Dicts;

public sealed record DictTypeListQuery(int Page = 1, int PageSize = 20, string? Keyword = null, string? Status = null);

public sealed record DictTypeSummaryDto(long Id, string Code, string Name, string? Description, bool IsSystem, string Status, int SortOrder, DateTimeOffset CreatedAt);

public sealed record DictTypeDetailDto(long Id, string Code, string Name, string? Description, bool IsSystem, string Status, int SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateDictTypeRequest(string Code, string Name, string? Description, int SortOrder, string Status);

public sealed record UpdateDictTypeRequest(string Name, string? Description, int SortOrder, string Status);

public sealed record DictValueDto(long Id, long TypeId, string TypeCode, string Label, string Value, string? Description, int SortOrder, bool IsDefault, string Status);

public sealed record CreateDictValueRequest(string Label, string Value, string? Description, int SortOrder, bool IsDefault, string Status);

public sealed record UpdateDictValueRequest(string Label, string Value, string? Description, int SortOrder, bool IsDefault, string Status);

public sealed record DictMutationResponse(long Id);

public interface IDictService
{
    Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListQuery query, CancellationToken cancellationToken);
    Task<DictTypeDetailDto> GetTypeAsync(long id, CancellationToken cancellationToken);
    Task<DictMutationResponse> CreateTypeAsync(CreateDictTypeRequest request, DictRequestContext context, CancellationToken cancellationToken);
    Task<DictMutationResponse> UpdateTypeAsync(long id, UpdateDictTypeRequest request, DictRequestContext context, CancellationToken cancellationToken);
    Task DeleteTypeAsync(long id, DictRequestContext context, CancellationToken cancellationToken);
    Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken);
    Task<DictMutationResponse> CreateValueAsync(string typeCode, CreateDictValueRequest request, DictRequestContext context, CancellationToken cancellationToken);
    Task<DictMutationResponse> UpdateValueAsync(long id, UpdateDictValueRequest request, DictRequestContext context, CancellationToken cancellationToken);
    Task DeleteValueAsync(long id, DictRequestContext context, CancellationToken cancellationToken);
}
