using WeCms.Shared;

namespace WeCms.Modules.System.Permissions;

public sealed class PermissionManagementService : IPermissionManagementService
{
    private readonly IPermissionRepository _repository;

    public PermissionManagementService(IPermissionRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PermissionSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return _repository.ListManagementAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionTreeDto>> TreeAsync(CancellationToken cancellationToken)
    {
        var permissions = await _repository.ListManagementAsync(cancellationToken);
        return permissions
            .GroupBy(permission => permission.Module, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PermissionTreeDto(
                group.Key,
                group.OrderBy(permission => permission.Code, StringComparer.Ordinal).ToArray()))
            .ToArray();
    }

    public async Task<PermissionDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetManagementAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Permission was not found.");
    }

    public async Task<PermissionMutationResponse> CreateAsync(CreatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 160);
        var name = NormalizeRequired(request.Name, "name", 160);
        var module = NormalizeRequired(request.Module, "module", 64);
        var description = NormalizeOptional(request.Description, 500);

        if (await _repository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "permission code already exists.");
        }

        var id = await _repository.CreateManagementAsync(new PermissionCreateRecord(code, name, module, description, context.Now), cancellationToken);
        await AuditAsync(context, "create", id, "success", "Permission created.", cancellationToken);
        return new PermissionMutationResponse(id);
    }

    public async Task<PermissionMutationResponse> UpdateAsync(long id, UpdatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var name = NormalizeRequired(request.Name, "name", 160);
        var module = NormalizeRequired(request.Module, "module", 64);
        var description = NormalizeOptional(request.Description, 500);

        await _repository.UpdateManagementAsync(new PermissionUpdateRecord(id, name, module, description, context.Now), cancellationToken);
        await AuditAsync(context, "update", id, "success", "Permission updated.", cancellationToken);
        return new PermissionMutationResponse(id);
    }

    public async Task DeleteAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken)
    {
        var permission = await GetAsync(id, cancellationToken);
        if (permission.IsBuiltin)
        {
            throw new DomainException(ApiCodes.BusinessError, "System built-in permissions cannot be deleted.");
        }

        await _repository.SoftDeleteManagementAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "success", permission.IsRoleBound ? "Role-bound permission soft deleted." : "Permission deleted.", cancellationToken);
    }

    public async Task EnableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetManagementStatusAsync(id, "enabled", context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "Permission enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetManagementStatusAsync(id, "disabled", context.Now, cancellationToken);
        await AuditAsync(context, "disable", id, "success", "Permission disabled.", cancellationToken);
    }

    private Task AuditAsync(PermissionRequestContext context, string action, long targetPermissionId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordManagementAuditAsync(
            new PermissionAuditRecord(context.ActorUserId, context.ActorUsername, action, targetPermissionId, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now),
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
