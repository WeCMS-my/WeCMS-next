namespace WeCms.Modules.AccessControl.Contracts;

public sealed record MenuSummaryDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    bool IsBuiltin);

public sealed record MenuTreeDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    bool IsBuiltin,
    IReadOnlyList<MenuTreeDto> Children);

public sealed record MenuDetailDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status,
    bool IsBuiltin,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateMenuRequest(
    long? ParentId,
    string Type,
    string Code,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status);

public sealed record UpdateMenuRequest(
    long? ParentId,
    string Type,
    string Path,
    string? Component,
    string Title,
    string? I18nKey,
    string? Icon,
    int Sort,
    bool Hidden,
    bool KeepAlive,
    string? ExternalUrl,
    string? PermissionCode,
    string Status);

public sealed record MenuMutationResponse(long Id);

public sealed record SortMenusRequest(IReadOnlyList<SortMenuItemRequest> Items);

public sealed record SortMenuItemRequest(long Id, long? ParentId, int Sort);
