namespace WeCms.Modules.AccessControl.Contracts;

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
