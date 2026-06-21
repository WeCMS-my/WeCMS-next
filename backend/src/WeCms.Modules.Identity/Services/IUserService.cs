using WeCms.Modules.Identity.Contracts;
using WeCms.Modules.Identity.Records;
using WeCms.Shared;

namespace WeCms.Modules.Identity.Services;

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

    Task AssignPositionsAsync(long id, AssignUserPositionsRequest request, UserRequestContext context, CancellationToken cancellationToken);
}
