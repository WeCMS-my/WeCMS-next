using Microsoft.AspNetCore.Http;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Modules.Identity.Services;

public sealed class AccountProfileService : IAccountProfileService
{
    private readonly IAccountProfileRepository _repository;
    private readonly IUserTwoFactorRepository _twoFactorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountAvatarFileService _avatarFileService;
    private readonly IUnitOfWork _unitOfWork;

    public AccountProfileService(
        IAccountProfileRepository repository,
        IUserTwoFactorRepository twoFactorRepository,
        IPasswordHasher passwordHasher,
        IAccountAvatarFileService avatarFileService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _twoFactorRepository = twoFactorRepository;
        _passwordHasher = passwordHasher;
        _avatarFileService = avatarFileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountProfileResponse> GetProfileAsync(AccountRequestContext context, CancellationToken cancellationToken)
    {
        return ToProfile(await RequiredUserAsync(context.UserId, cancellationToken));
    }

    public async Task<AccountProfileResponse> UpdateProfileAsync(UpdateAccountProfileRequest request, AccountRequestContext context, CancellationToken cancellationToken)
    {
        var displayName = NormalizeRequired(request.DisplayName, "displayName", 120);
        var email = NormalizeOptional(request.Email, 160);
        var phone = NormalizeOptional(request.Phone, 40);
        if (email is not null && await _repository.EmailExistsAsync(email, context.UserId, cancellationToken))
        {
            throw Validation("email already exists.");
        }

        if (phone is not null && await _repository.PhoneExistsAsync(phone, context.UserId, cancellationToken))
        {
            throw Validation("phone already exists.");
        }

        await _repository.UpdateProfileAsync(new AccountProfileUpdateRecord(context.UserId, displayName, email, phone, context.Now), cancellationToken);
        await AuditAsync(context, "profile-update", "success", "Account profile updated.", cancellationToken);
        await SecurityEventAsync(context, "auth.account_profile_updated", "info", "Account profile updated.", cancellationToken);
        return await GetProfileAsync(context, cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangeAccountPasswordRequest request, AccountRequestContext context, CancellationToken cancellationToken)
    {
        var user = await RequiredUserAsync(context.UserId, cancellationToken);
        var oldPassword = NormalizeRequired(request.OldPassword, "oldPassword", 128);
        var newPassword = NormalizeRequired(request.NewPassword, "newPassword", 128);
        EnsurePasswordPolicy(newPassword);
        if (!_passwordHasher.Verify(oldPassword, user.PasswordHash))
        {
            await SecurityEventAsync(context, "auth.account_password_change_rejected", "warning", "Account password change rejected.", cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Old password is invalid.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.UpdatePasswordAsync(new AccountPasswordUpdateRecord(context.UserId, _passwordHasher.Hash(newPassword), context.Now), cancellationToken);
            await _repository.RevokeRefreshTokensAsync(context.UserId, context.Now, cancellationToken);
            await AuditAsync(context, "password-change", "success", "Account password changed and refresh tokens revoked.", cancellationToken);
            await SecurityEventAsync(context, "auth.account_password_changed", "warning", "Account password changed.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AccountAvatarResponse> UploadAvatarAsync(AccountAvatarUploadRequest request, IFormFile file, AccountRequestContext context, CancellationToken cancellationToken)
    {
        var user = await RequiredUserAsync(context.UserId, cancellationToken);
        AccountAvatarStoredFile? stored = null;
        try
        {
            stored = await _avatarFileService.StoreAsync(request, file, context.Now, cancellationToken);
            try
            {
                await _repository.UpdateAvatarAsync(new AccountAvatarUpdateRecord(context.UserId, stored.ObjectKey, stored.MimeType, stored.FileExtension, context.Now), cancellationToken);
            }
            catch
            {
                await _avatarFileService.DeleteAsync(stored.ObjectKey, cancellationToken);
                throw;
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarObjectKey))
            {
                await _avatarFileService.DeleteAsync(user.AvatarObjectKey, cancellationToken);
            }

            await AuditAsync(context, "avatar-update", "success", "Account avatar updated.", cancellationToken);
            await SecurityEventAsync(context, "auth.account_avatar_updated", "info", "Account avatar updated.", cancellationToken);
            return new AccountAvatarResponse(AvatarUrl());
        }
        catch (DomainException exception)
        {
            await SecurityEventAsync(context, "file_upload_rejected", "warning", $"Avatar upload rejected: {exception.Message}", cancellationToken);
            throw;
        }
    }

    public async Task<AccountAvatarDownload> GetAvatarAsync(AccountRequestContext context, CancellationToken cancellationToken)
    {
        var user = await RequiredUserAsync(context.UserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user.AvatarObjectKey) || string.IsNullOrWhiteSpace(user.AvatarMimeType))
        {
            throw new DomainException(ApiCodes.NotFound, "Avatar was not found.");
        }

        return await _avatarFileService.OpenAsync(user.AvatarObjectKey, user.AvatarMimeType, user.AvatarFileExt ?? string.Empty, cancellationToken);
    }

    public async Task<AccountSecurityResponse> GetSecurityAsync(AccountRequestContext context, CancellationToken cancellationToken)
    {
        var user = await RequiredUserAsync(context.UserId, cancellationToken);
        var twoFactor = await _twoFactorRepository.GetByUserIdAsync(context.UserId, cancellationToken);
        return new AccountSecurityResponse(twoFactor?.Enabled == true, twoFactor?.ResetRequired == true, user.MustChangePassword, user.LastLoginAt, user.LastLoginIp);
    }

    private async Task<AccountProfileRecord> RequiredUserAsync(long userId, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(userId, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Account was not found.");
    }

    private Task AuditAsync(AccountRequestContext context, string action, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new AccountAuditRecord(context.UserId, context.Username, action, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now), cancellationToken);
    }

    private Task SecurityEventAsync(AccountRequestContext context, string eventType, string severity, string message, CancellationToken cancellationToken)
    {
        return _repository.RecordSecurityEventAsync(new AccountSecurityEventRecord(eventType, context.UserId, context.Username, context.Ip, severity, message, context.Now, context.TraceId), cancellationToken);
    }

    private static AccountProfileResponse ToProfile(AccountProfileRecord user)
    {
        return new AccountProfileResponse(user.Id, user.Username, user.DisplayName, user.Email, user.Phone, string.IsNullOrWhiteSpace(user.AvatarObjectKey) ? null : AvatarUrl());
    }

    private static string AvatarUrl() => "/api/v1/account/avatar/content";

    private static void EnsurePasswordPolicy(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw Validation("newPassword must be at least 8 characters and include upper, lower, digit, and symbol characters.");
        }
    }

    private static string NormalizeRequired(string? value, string name, int maxLength) => NormalizeOptional(value, maxLength) ?? throw Validation($"{name} is required.");

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
