using WeCms.Shared;

namespace WeCms.Modules.System.Users;

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? Status = null,
    long? DeptId = null);

public sealed record UserSummaryDto(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId,
    string Status,
    bool IsSuperAdmin,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record UserDetailDto(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId,
    string Status,
    bool IsSuperAdmin,
    long PermissionVersion,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<long> RoleIds,
    IReadOnlyList<long> PostIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Password,
    string? Email,
    string? Phone,
    long? DeptId,
    IReadOnlyList<long>? RoleIds,
    IReadOnlyList<long>? PostIds);

public sealed record UpdateUserRequest(
    string DisplayName,
    string? Email,
    string? Phone,
    long? DeptId);

public sealed record ResetUserPasswordRequest(string Password);

public sealed record ResetUserTwoFactorRequest(string Reason);

public sealed record AssignUserRolesRequest(IReadOnlyList<long> RoleIds);

public sealed record AssignUserPostsRequest(IReadOnlyList<long> PostIds);

public sealed record UserMutationResponse(long Id);

public interface IUserService
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<UserDetailDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<UserMutationResponse> CreateAsync(CreateUserRequest request, UserRequestContext context, CancellationToken cancellationToken);

    Task<UserMutationResponse> UpdateAsync(long id, UpdateUserRequest request, UserRequestContext context, CancellationToken cancellationToken);

    Task DeleteAsync(long id, UserRequestContext context, CancellationToken cancellationToken);

    Task EnableAsync(long id, UserRequestContext context, CancellationToken cancellationToken);

    Task DisableAsync(long id, UserRequestContext context, CancellationToken cancellationToken);

    Task ResetPasswordAsync(long id, ResetUserPasswordRequest request, UserRequestContext context, CancellationToken cancellationToken);

    Task ResetTwoFactorAsync(long id, ResetUserTwoFactorRequest request, UserRequestContext context, CancellationToken cancellationToken);

    Task AssignRolesAsync(long id, AssignUserRolesRequest request, UserRequestContext context, CancellationToken cancellationToken);

    Task AssignPostsAsync(long id, AssignUserPostsRequest request, UserRequestContext context, CancellationToken cancellationToken);
}
