namespace WeCms.Modules.System.Menus;

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
    string Status);

public sealed record MenuMutationResponse(long Id);

public interface IMenuService
{
    Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MenuTreeDto>> TreeAsync(CancellationToken cancellationToken);
    Task<MenuDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<MenuMutationResponse> CreateAsync(CreateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken);
    Task<MenuMutationResponse> UpdateAsync(long id, UpdateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);
    Task EnableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);
    Task DisableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);
}
