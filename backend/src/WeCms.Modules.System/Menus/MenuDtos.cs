namespace WeCms.Modules.System.Menus;

public sealed record MenuTreeItem(long Id, long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? Icon, int Sort, bool Hidden, string Status, List<MenuTreeItem> Children);
public sealed record MenuDetail(long Id, long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? I18nKey, string? Icon, int Sort, bool Hidden, bool KeepAlive, string? ExternalUrl, string? PermissionCode, string Status, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateMenuRequest(long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? Icon, int Sort = 0, bool Hidden = false, string? PermissionCode = null, string? I18nKey = null, bool KeepAlive = false, string? ExternalUrl = null);
// TODO M13: ParentId cannot be set to NULL via this API. A sentinel value (e.g. -1) would be needed for that.
public sealed record UpdateMenuRequest(string? Title, string? Path, string? Component, string? Icon, int? Sort, bool? Hidden, long? ParentId = null);
public sealed record MenuSortRequest(long[] OrderedIds);
