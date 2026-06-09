namespace WeCms.Modules.System.Settings;

public interface ISettingService
{
    Task<List<SettingItem>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(string key, string value, CancellationToken ct);
}
