using WeCms.Shared;

namespace WeCms.Modules.System.Menus;

public sealed class MenuService : IMenuService
{
    private readonly IMenuRepository _repository;

    public MenuService(IMenuRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return _repository.ListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuTreeDto>> TreeAsync(CancellationToken cancellationToken)
    {
        var menus = await _repository.ListAsync(cancellationToken);
        return MenuTreeBuilder.Build(menus);
    }

    public async Task<MenuDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Menu was not found.");
    }

    public async Task<MenuMutationResponse> CreateAsync(CreateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 120);
        await EnsureCodeUniqueAsync(code, null, cancellationToken);
        await EnsureParentAsync(null, request.ParentId, cancellationToken);
        var type = NormalizeType(request.Type);
        var status = NormalizeStatus(request.Status);

        var id = await _repository.CreateAsync(
            new MenuCreateRecord(
                request.ParentId,
                type,
                code,
                NormalizeRequired(request.Path, "path", 240),
                NormalizeOptional(request.Component, 240),
                NormalizeRequired(request.Title, "title", 120),
                NormalizeOptional(request.I18nKey, 160),
                NormalizeOptional(request.Icon, 120),
                request.Sort,
                request.Hidden,
                request.KeepAlive,
                NormalizeOptional(request.ExternalUrl, 500),
                NormalizeOptional(request.PermissionCode, 160),
                status,
                context.Now),
            cancellationToken);
        await AuditAsync(context, "create", id, "success", "Menu created.", cancellationToken);
        return new MenuMutationResponse(id);
    }

    public async Task<MenuMutationResponse> UpdateAsync(long id, UpdateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await EnsureParentAsync(id, request.ParentId, cancellationToken);
        var type = NormalizeType(request.Type);
        var status = NormalizeStatus(request.Status);

        await _repository.UpdateAsync(
            new MenuUpdateRecord(
                id,
                request.ParentId,
                type,
                NormalizeRequired(request.Path, "path", 240),
                NormalizeOptional(request.Component, 240),
                NormalizeRequired(request.Title, "title", 120),
                NormalizeOptional(request.I18nKey, 160),
                NormalizeOptional(request.Icon, 120),
                request.Sort,
                request.Hidden,
                request.KeepAlive,
                NormalizeOptional(request.ExternalUrl, 500),
                NormalizeOptional(request.PermissionCode, 160),
                status,
                context.Now),
            cancellationToken);
        await AuditAsync(context, "update", id, "success", "Menu updated.", cancellationToken);
        return new MenuMutationResponse(id);
    }

    public async Task DeleteAsync(long id, MenuRequestContext context, CancellationToken cancellationToken)
    {
        var menu = await GetAsync(id, cancellationToken);
        if (menu.IsBuiltin)
        {
            throw new DomainException(ApiCodes.BusinessError, "System built-in menus cannot be deleted.");
        }

        if (await _repository.HasChildrenAsync(id, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Menu with child menus cannot be deleted.");
        }

        await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "success", "Menu deleted.", cancellationToken);
    }

    public async Task EnableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken)
    {
        var menu = await GetAsync(id, cancellationToken);
        EnsureNotBuiltin(menu.IsBuiltin, "enable");
        await _repository.SetStatusAsync(id, "enabled", context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "Menu enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken)
    {
        var menu = await GetAsync(id, cancellationToken);
        EnsureNotBuiltin(menu.IsBuiltin, "disable");
        await _repository.SetStatusAsync(id, "disabled", context.Now, cancellationToken);
        await AuditAsync(context, "disable", id, "success", "Menu disabled.", cancellationToken);
    }

    private static void EnsureNotBuiltin(bool isBuiltin, string action)
    {
        if (isBuiltin)
        {
            throw new DomainException(ApiCodes.BusinessError, $"System built-in menus cannot be {action}d.");
        }
    }

    private async Task EnsureParentAsync(long? currentMenuId, long? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        if (parentId <= 0)
        {
            throw Validation("parentId must be positive.");
        }

        if (currentMenuId == parentId)
        {
            throw new DomainException(ApiCodes.BusinessError, "Menu cannot be its own parent.");
        }

        if (!await _repository.ExistsAsync(parentId.Value, cancellationToken))
        {
            throw Validation("parentId does not exist.");
        }

        if (currentMenuId is not null && await _repository.IsDescendantAsync(currentMenuId.Value, parentId.Value, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Menu parent cannot be a descendant.");
        }
    }

    private async Task EnsureCodeUniqueAsync(string code, long? exceptMenuId, CancellationToken cancellationToken)
    {
        if (await _repository.CodeExistsAsync(code, exceptMenuId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "menu code already exists.");
        }
    }

    private Task AuditAsync(MenuRequestContext context, string action, long targetMenuId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(
            new MenuAuditRecord(context.ActorUserId, context.ActorUsername, action, targetMenuId, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now),
            cancellationToken);
    }

    private static string NormalizeType(string value)
    {
        var normalized = NormalizeRequired(value, "type", 32);
        return normalized is "catalog" or "menu" or "button"
            ? normalized
            : throw Validation("type must be catalog, menu, or button.");
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value, "status", 32);
        return normalized is "enabled" or "disabled"
            ? normalized
            : throw Validation("status must be enabled or disabled.");
    }

    private static string NormalizeRequired(string? value, string name, int maxLength)
    {
        var normalized = NormalizeOptional(value, maxLength);
        return normalized ?? throw Validation($"{name} is required.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw Validation($"value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static DomainException Validation(string message)
    {
        return new DomainException(ApiCodes.ValidationError, message);
    }
}
