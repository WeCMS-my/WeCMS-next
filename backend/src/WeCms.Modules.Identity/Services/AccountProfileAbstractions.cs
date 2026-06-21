using Microsoft.AspNetCore.Http;

namespace WeCms.Modules.Identity.Services;

public interface IAccountProfileService
{
    Task<AccountProfileResponse> GetProfileAsync(AccountRequestContext context, CancellationToken cancellationToken);

    Task<AccountProfileResponse> UpdateProfileAsync(UpdateAccountProfileRequest request, AccountRequestContext context, CancellationToken cancellationToken);

    Task ChangePasswordAsync(ChangeAccountPasswordRequest request, AccountRequestContext context, CancellationToken cancellationToken);

    Task<AccountAvatarResponse> UploadAvatarAsync(AccountAvatarUploadRequest request, IFormFile file, AccountRequestContext context, CancellationToken cancellationToken);

    Task<AccountAvatarDownload> GetAvatarAsync(AccountRequestContext context, CancellationToken cancellationToken);

    Task<AccountSecurityResponse> GetSecurityAsync(AccountRequestContext context, CancellationToken cancellationToken);
}
