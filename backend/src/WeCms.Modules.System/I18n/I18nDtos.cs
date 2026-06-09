 namespace WeCms.Modules.System.I18n;
 
 public sealed record I18nMessageItem(long Id, string Locale, string MessageKey, string MessageValue, string? Remark);
 public sealed record CreateI18nRequest(string Locale, string MessageKey, string MessageValue, string? Remark);
 public sealed record UpdateI18nRequest(string? MessageValue, string? Remark);
