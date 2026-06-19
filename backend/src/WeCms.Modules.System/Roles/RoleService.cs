using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Modules.System.Permissions;

namespace WeCms.Modules.System.Roles;

public sealed class RoleService : IRoleService
{
    private const int MaxPageSize = 100;
    private const int MaxAssignmentCount = 200;
    private const string SuperAdminCode = "super_admin";
    private readonly IRoleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionVersionService _permissionVersionService;

    public RoleService(IRoleRepository repository, IUnitOfWork unitOfWork, IPermissionVersionService permissionVersionService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _permissionVersionService = permissionVersionService;
    }

    public Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize
            ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.")
            : query.PageSize;
        var status = NormalizeOptional(query.Status, 32);
        if (status is not null && status is not "enabled" and not "disabled")
        {
            throw Validation("status must be enabled or disabled.");
        }

        return _repository.ListAsync(
            new RoleListCriteria(page, pageSize, NormalizeOptional(query.Keyword, 80), status),
            cancellationToken);
    }

    public async Task<RoleDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Role was not found.");
    }

    public async Task<RoleMutationResponse> CreateAsync(CreateRoleRequest request, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 64);
        var name = NormalizeRequired(request.Name, "name", 120);
        await EnsureCodeUniqueAsync(code, null, cancellationToken);
        var permissionIds = await EnsureExistingIdsAsync(request.PermissionIds ?? [], _repository.ExistingPermissionIdsAsync, "permissionIds", cancellationToken);
        var menuIds = await EnsureExistingIdsAsync(request.MenuIds ?? [], _repository.ExistingMenuIdsAsync, "menuIds", cancellationToken);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var roleId = await _repository.CreateAsync(new RoleCreateRecord(code, name, context.Now), cancellationToken);
            if (permissionIds.Count > 0)
            {
                await _repository.ReplacePermissionsAsync(roleId, permissionIds, context.Now, cancellationToken);
            }

            if (menuIds.Count > 0)
            {
                await _repository.ReplaceMenusAsync(roleId, menuIds, context.Now, cancellationToken);
            }

            await AuditAsync(context, "create", roleId, "success", "Role created.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RoleMutationResponse(roleId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RoleMutationResponse> UpdateAsync(long id, UpdateRoleRequest request, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role cannot be updated.");
        var name = NormalizeRequired(request.Name, "name", 120);
        await _repository.UpdateAsync(new RoleUpdateRecord(id, name, context.Now), cancellationToken);
        await AuditAsync(context, "update", id, "success", "Role updated.", cancellationToken);
        return new RoleMutationResponse(id);
    }

    public async Task DeleteAsync(long id, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role cannot be deleted.");
        EnsureCanDelete(role);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
            await _permissionVersionService.BumpUsersByRoleAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "delete", id, "success", "Role deleted.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task EnableAsync(long id, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role cannot be enabled.");
        await _repository.SetStatusAsync(id, "enabled", context.Now, cancellationToken);
        await _permissionVersionService.BumpUsersByRoleAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "Role enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role cannot be disabled.");
        EnsureNotSuperAdmin(role, "disable");
        await _repository.SetStatusAsync(id, "disabled", context.Now, cancellationToken);
        await _permissionVersionService.BumpUsersByRoleAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "disable", id, "success", "Role disabled.", cancellationToken);
    }

    public async Task AssignPermissionsAsync(long id, AssignRolePermissionsRequest request, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role permissions cannot be modified.");
        var permissionIds = await EnsureExistingIdsAsync(request.PermissionIds, _repository.ExistingPermissionIdsAsync, "permissionIds", cancellationToken);
        if (IsSuperAdmin(role) && permissionIds.Count == 0)
        {
            throw new DomainException(ApiCodes.BusinessError, "Cannot remove all super_admin permissions.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.ReplacePermissionsAsync(id, permissionIds, context.Now, cancellationToken);
            await _permissionVersionService.BumpUsersByRoleAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "assign-permission", id, "success", "Role permissions assigned.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignMenusAsync(long id, AssignRoleMenusRequest request, RoleRequestContext context, CancellationToken cancellationToken)
    {
        var role = await GetAsync(id, cancellationToken);
        EnsureRoleNotLocked(role, "Locked role menus cannot be modified.");
        var menuIds = await EnsureExistingIdsAsync(request.MenuIds, _repository.ExistingMenuIdsAsync, "menuIds", cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.ReplaceMenusAsync(id, menuIds, context.Now, cancellationToken);
            await _permissionVersionService.BumpUsersByRoleAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "assign-menu", id, "success", "Role menus assigned.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureCodeUniqueAsync(string code, long? exceptRoleId, CancellationToken cancellationToken)
    {
        if (await _repository.CodeExistsAsync(code, exceptRoleId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "role code already exists.");
        }
    }

    private static async Task<IReadOnlyList<long>> EnsureExistingIdsAsync(
        IReadOnlyList<long> ids,
        Func<IReadOnlyList<long>, CancellationToken, Task<IReadOnlySet<long>>> existingFactory,
        string fieldName,
        CancellationToken cancellationToken)
    {
        if (ids.Count > MaxAssignmentCount)
        {
            throw Validation($"{fieldName} cannot contain more than {MaxAssignmentCount} ids.");
        }

        if (ids.Any(id => id <= 0))
        {
            throw Validation($"{fieldName} must contain positive ids.");
        }

        var normalized = ids.Distinct().Order().ToArray();
        var existing = await existingFactory(normalized, cancellationToken);
        if (existing.Count != normalized.Length)
        {
            throw Validation($"{fieldName} contains unknown ids.");
        }

        return normalized;
    }

    private static void EnsureCanDelete(RoleDetailDto role)
    {
        if (role.IsBuiltin)
        {
            throw new DomainException(ApiCodes.BusinessError, "System built-in roles cannot be deleted.");
        }

        EnsureNotSuperAdmin(role, "delete");
    }

    private static void EnsureRoleNotLocked(RoleDetailDto role, string message)
    {
        if (role.IsLocked)
        {
            throw new DomainException(ApiCodes.BusinessError, message);
        }
    }

    private static void EnsureNotSuperAdmin(RoleDetailDto role, string action)
    {
        if (IsSuperAdmin(role))
        {
            throw new DomainException(ApiCodes.BusinessError, $"Cannot {action} super_admin.");
        }
    }

    private static bool IsSuperAdmin(RoleDetailDto role)
    {
        return string.Equals(role.Code, SuperAdminCode, StringComparison.Ordinal);
    }

    private Task AuditAsync(RoleRequestContext context, string action, long targetRoleId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(
            new RoleAuditRecord(
                context.ActorUserId,
                context.ActorUsername,
                action,
                targetRoleId,
                context.Ip,
                context.UserAgent,
                context.TraceId,
                result,
                detail,
                context.Now),
            cancellationToken);
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
