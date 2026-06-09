 namespace WeCms.Modules.System.Settings;
 
 public sealed record SettingItem(string Key, string Value, string? Group, string? Description, bool IsSensitive);
 public sealed record UpdateSettingRequest(string Value);
