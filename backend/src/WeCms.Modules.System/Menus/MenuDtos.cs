 namespace WeCms.Modules.System.Menus;
 
 public sealed record MenuTreeItem(long Id, long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? Icon, int Sort, bool Hidden, string Status, List<MenuTreeItem> Children);
 public sealed record MenuDetail(long Id, long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? I18nKey, string? Icon, int Sort, bool Hidden, bool KeepAlive, string? ExternalUrl, string? PermissionCode, string Status, DateTime CreatedAt, DateTime UpdatedAt);
 public sealed record CreateMenuRequest(long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? Icon, int Sort = 0, bool Hidden = false, string? PermissionCode = null);
 public sealed record UpdateMenuRequest(string? Title, string? Path, string? Component, string? Icon, int? Sort, bool? Hidden);
 public sealed record MenuSortRequest(long[] OrderedIds);
