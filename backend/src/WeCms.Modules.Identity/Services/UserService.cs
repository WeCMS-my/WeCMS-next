using WeCms.EventBus;
using WeCms.Modules.Identity.Events;
using WeCms.Modules.Organization;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Id;

namespace WeCms.Modules.Identity.Services;

public sealed class UserService : IUserService
{
    private const int MaxPageSize = 100;
    private const int MaxAssignmentCount = 100;
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IIdentityPermissionVersionService _permissionVersionService;
    private readonly IOrganizationLookupService _organizationLookupService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IIdGenerator _idGenerator;

    public UserService(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ITwoFactorService twoFactorService,
        IIdentityPermissionVersionService permissionVersionService,
        IOrganizationLookupService organizationLookupService,
        IOutboxWriter outboxWriter,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _twoFactorService = twoFactorService;
        _permissionVersionService = permissionVersionService;
        _organizationLookupService = organizationLookupService;
        _outboxWriter = outboxWriter;
        _idGenerator = idGenerator;
    }

    public Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
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

        var criteria = new UserListCriteria(
            page,
            pageSize,
            NormalizeOptional(query.Keyword, 80),
            status,
            query.DeptId);

        return _repository.ListAsync(criteria, cancellationToken);
    }

    public async Task<UserDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "User was not found.");
    }

    public async Task<UserMutationResponse> CreateAsync(CreateUserRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        var username = NormalizeRequired(request.Username, "username", 64);
        var displayName = NormalizeRequired(request.DisplayName, "displayName", 120);
        var password = NormalizeRequired(request.Password, "password", 128);
        var email = NormalizeOptional(request.Email, 160);
        var phone = NormalizeOptional(request.Phone, 40);

        await EnsureUniqueAsync(username, email, phone, null, cancellationToken);
        await EnsureDeptAsync(request.DeptId, cancellationToken);
        var roleIds = await EnsureExistingIdsAsync(request.RoleIds ?? [], _repository.ExistingRoleIdsAsync, "roleIds", cancellationToken);
        var positionIds = await EnsureExistingIdsAsync(request.PositionIds ?? [], _organizationLookupService.ExistingPositionIdsAsync, "positionIds", cancellationToken);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var userId = await _repository.CreateAsync(
                new UserCreateRecord(username, displayName, _passwordHasher.Hash(password), email, phone, request.DeptId, context.Now),
                cancellationToken);
            if (roleIds.Count > 0)
            {
                await _repository.ReplaceRolesAsync(userId, roleIds, context.Now, cancellationToken);
            }

            if (positionIds.Count > 0)
            {
                await _repository.ReplacePositionsAsync(userId, positionIds, context.Now, cancellationToken);
            }

            await AuditAsync(context, "create", userId, "success", "User created.", cancellationToken);
            await _outboxWriter.WriteAsync(new UserCreatedEvent(NewEventId(), context.Now, context.TraceId, null, userId), cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UserMutationResponse(userId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserMutationResponse> UpdateAsync(long id, UpdateUserRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var displayName = NormalizeRequired(request.DisplayName, "displayName", 120);
        var email = NormalizeOptional(request.Email, 160);
        var phone = NormalizeOptional(request.Phone, 40);

        await EnsureUniqueAsync(null, email, phone, id, cancellationToken);
        await EnsureDeptAsync(request.DeptId, cancellationToken);
        await _repository.UpdateAsync(new UserUpdateRecord(id, displayName, email, phone, request.DeptId, context.Now), cancellationToken);
        await AuditAsync(context, "update", id, "success", "User updated.", cancellationToken);

        return new UserMutationResponse(id);
    }

    public async Task DeleteAsync(long id, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        EnsureNotSelf(id, context.ActorUserId, "delete");
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureLockedRolesStillHaveEnabledHolderAsync(id, null, cancellationToken);
            await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
            await _repository.RevokeUserRefreshTokensAsync(id, context.Now, cancellationToken);
            await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "delete", id, "success", "User deleted.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task EnableAsync(long id, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetStatusAsync(id, "enabled", context.Now, cancellationToken);
        await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "User enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        EnsureNotSelf(id, context.ActorUserId, "disable");
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureLockedRolesStillHaveEnabledHolderAsync(id, null, cancellationToken);
            await _repository.SetStatusAsync(id, "disabled", context.Now, cancellationToken);
            await _repository.RevokeUserRefreshTokensAsync(id, context.Now, cancellationToken);
            await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "disable", id, "success", "User disabled.", cancellationToken);
            await _outboxWriter.WriteAsync(new UserDisabledEvent(NewEventId(), context.Now, context.TraceId, null, id), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ResetPasswordAsync(long id, ResetUserPasswordRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var password = NormalizeRequired(request.Password, "password", 128);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.ResetPasswordAsync(id, _passwordHasher.Hash(password), context.Now, cancellationToken);
            await _repository.RevokeUserRefreshTokensAsync(id, context.Now, cancellationToken);
            await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "reset-password", id, "success", "User password reset.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ResetTwoFactorAsync(long id, ResetUserTwoFactorRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        var user = await GetAsync(id, cancellationToken);
        var reason = NormalizeRequired(request.Reason, "reason", 200);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _twoFactorService.ClearAsync(id, context.Now, cancellationToken);
            await _repository.RevokeUserRefreshTokensAsync(id, context.Now, cancellationToken);
            await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "reset-2fa", id, "success", $"User two-factor authentication reset. Reason: {reason}", cancellationToken);
            await RecordSecurityEventAsync(context, user, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignRolesAsync(long id, AssignUserRolesRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var roleIds = await EnsureExistingIdsAsync(request.RoleIds, _repository.ExistingRoleIdsAsync, "roleIds", cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureLockedRolesStillHaveEnabledHolderAsync(id, roleIds, cancellationToken);
            await _repository.ReplaceRolesAsync(id, roleIds, context.Now, cancellationToken);
            await _permissionVersionService.BumpUserAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "assign-role", id, "success", "User roles assigned.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignPositionsAsync(long id, AssignUserPositionsRequest request, UserRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var positionIds = await EnsureExistingIdsAsync(request.PositionIds, _organizationLookupService.ExistingPositionIdsAsync, "positionIds", cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.ReplacePositionsAsync(id, positionIds, context.Now, cancellationToken);
            await AuditAsync(context, "assign-position", id, "success", "User positions assigned.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureUniqueAsync(string? username, string? email, string? phone, long? exceptUserId, CancellationToken cancellationToken)
    {
        if (username is not null && await _repository.UsernameExistsAsync(username, exceptUserId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "username already exists.");
        }

        if (email is not null && await _repository.EmailExistsAsync(email, exceptUserId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "email already exists.");
        }

        if (phone is not null && await _repository.PhoneExistsAsync(phone, exceptUserId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "phone already exists.");
        }
    }

    private async Task EnsureDeptAsync(long? deptId, CancellationToken cancellationToken)
    {
        if (deptId is not null && !await _organizationLookupService.DepartmentExistsAsync(deptId.Value, cancellationToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "deptId does not exist.");
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

    private static void EnsureNotSelf(long targetUserId, long actorUserId, string action)
    {
        if (targetUserId == actorUserId)
        {
            throw new DomainException(ApiCodes.BusinessError, $"Cannot {action} yourself.");
        }
    }

    private async Task EnsureLockedRolesStillHaveEnabledHolderAsync(
        long targetUserId,
        IReadOnlyList<long>? newRoleIds,
        CancellationToken cancellationToken)
    {
        var currentLockedRoleIds = await _repository.ListLockedRoleIdsByUserAsync(targetUserId, cancellationToken);
        if (currentLockedRoleIds.Count == 0)
        {
            return;
        }

        IReadOnlySet<long> affectedLockedRoleIds;
        if (newRoleIds is null)
        {
            affectedLockedRoleIds = currentLockedRoleIds.ToHashSet();
        }
        else
        {
            var newLockedRoleIds = await _repository.ExistingLockedRoleIdsAsync(newRoleIds, cancellationToken);
            affectedLockedRoleIds = currentLockedRoleIds.Except(newLockedRoleIds).ToHashSet();
        }

        foreach (var roleId in affectedLockedRoleIds)
        {
            if (await _repository.CountEnabledUsersByRoleForUpdateAsync(roleId, cancellationToken) <= 1)
            {
                throw new DomainException(ApiCodes.BusinessError, "Locked role must have at least one enabled user.");
            }
        }
    }

    private Task AuditAsync(UserRequestContext context, string action, long targetUserId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(
            new UserAuditRecord(
                context.ActorUserId,
                context.ActorUsername,
                action,
                targetUserId,
                context.Ip,
                context.UserAgent,
                context.TraceId,
                result,
                detail,
                context.Now),
            cancellationToken);
    }

    private Task RecordSecurityEventAsync(
        UserRequestContext context,
        UserDetailDto targetUser,
        string reason,
        CancellationToken cancellationToken)
    {
        return _repository.RecordSecurityEventAsync(
            new UserSecurityEventRecord(
                "auth.user_2fa_reset",
                targetUser.Id,
                targetUser.Username,
                context.Ip,
                "warning",
                $"Administrator reset user two-factor authentication. Reason: {reason}",
                context.Now,
                context.TraceId),
            cancellationToken);
    }

    private Guid NewEventId()
    {
        return Guid.ParseExact(_idGenerator.NewId(), "N");
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
