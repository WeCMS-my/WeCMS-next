namespace WeCms.Modules.System.Permissions;

public sealed record PermissionSummaryDto(
    long Id,
    string Code,
    string Name,
    string Module,
    string? Description,
    string Status,
    bool IsBuiltin,
    bool IsRoleBound);

public sealed record PermissionTreeDto(
    string Module,
    IReadOnlyList<PermissionSummaryDto> Permissions);

public sealed record PermissionDetailDto(
    long Id,
    string Code,
    string Name,
    string Module,
    string? Description,
    string Status,
    bool IsBuiltin,
    bool IsRoleBound,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePermissionRequest(string Code, string Name, string Module, string? Description);

public sealed record UpdatePermissionRequest(string Name, string Module, string? Description);

public sealed record PermissionMutationResponse(long Id);

public interface IPermissionManagementService
{
    Task<IReadOnlyList<PermissionSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionTreeDto>> TreeAsync(CancellationToken cancellationToken);
    Task<PermissionDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<PermissionMutationResponse> CreateAsync(CreatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken);
    Task<PermissionMutationResponse> UpdateAsync(long id, UpdatePermissionRequest request, PermissionRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);
    Task EnableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);
    Task DisableAsync(long id, PermissionRequestContext context, CancellationToken cancellationToken);
}
