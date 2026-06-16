using WeCms.Shared;

namespace WeCms.Modules.System.Settings;

public sealed record SettingListQuery(int Page = 1, int PageSize = 20, string? Keyword = null, string? GroupCode = null);

public sealed record SettingSummaryDto(string Key, string? Value, string ValueType, string GroupCode, string Name, string? Description, bool IsSensitive, bool IsSystem, DateTimeOffset UpdatedAt, long? UpdatedBy);

public sealed record SettingDetailDto(string Key, string? Value, string ValueType, string GroupCode, string Name, string? Description, bool IsSensitive, bool IsSystem, DateTimeOffset UpdatedAt, long? UpdatedBy);

public sealed record UpdateSettingRequest(string? Value);

public sealed record SettingMutationResponse(string Key);

public interface ISettingService
{
    Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListQuery query, CancellationToken cancellationToken);
    Task<SettingDetailDto> GetAsync(string key, CancellationToken cancellationToken);
    Task<SettingMutationResponse> UpdateAsync(string key, UpdateSettingRequest request, SettingRequestContext context, CancellationToken cancellationToken);
}
