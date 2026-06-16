using WeCms.Shared;

namespace WeCms.Modules.System.Settings;

public interface ISettingRepository
{
    Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListCriteria criteria, CancellationToken cancellationToken);
    Task<SettingDetailDto?> GetAsync(string key, CancellationToken cancellationToken);
    Task UpdateAsync(SettingUpdateRecord record, CancellationToken cancellationToken);
    Task RecordAuditAsync(SettingAuditRecord record, CancellationToken cancellationToken);
}
