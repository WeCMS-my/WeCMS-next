namespace WeCms.Modules.System.Settings;

public interface ISettingService
{
    Task<(IReadOnlyList<SettingItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct);
    Task UpdateAsync(string key, string value, CancellationToken ct);
}
