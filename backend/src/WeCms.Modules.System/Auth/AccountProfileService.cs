using Microsoft.AspNetCore.Http;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Modules.System.Auth;

public sealed class AccountProfileService : IAccountProfileService
{
    private readonly IAccountProfileRepository _repository;
    private readonly IUserTwoFactorRepository _twoFactorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFileStorage _storage;
    private readonly IFileObjectKeyGenerator _objectKeyGenerator;
    private readonly IFileUploadPolicyResolver _policyResolver;
    private readonly IUnitOfWork _unitOfWork;

    public AccountProfileService(
        IAccountProfileRepository repository,
        IUserTwoFactorRepository twoFactorRepository,
        IPasswordHasher passwordHasher,
        IFileStorage storage,
        IFileObjectKeyGenerator objectKeyGenerator,
        IFileUploadPolicyResolver policyResolver,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _twoFactorRepository = twoFactorRepository;
        _passwordHasher = passwordHasher;
        _storage = storage;
        _objectKeyGenerator = objectKeyGenerator;
        _policyResolver = policyResolver;
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
        var originalName = NormalizeRequired(request.OriginalName, "originalName", 255);
        var mimeType = NormalizeRequired(request.MimeType, "mimeType", 120);
        var sha256 = NormalizeSha256(request.Sha256);
        var ext = Path.GetExtension(originalName);
        var policy = _policyResolver.Resolve("avatar");
        if (request.SizeBytes <= 0 || request.SizeBytes > policy.MaxSizeBytes)
        {
            throw Validation($"Avatar size must be between 1 and {policy.MaxSizeBytes} bytes.");
        }

        await policy.ValidateContentAsync(file, mimeType, ext, cancellationToken);

        var objectKey = $"{policy.StorageScope}/{_objectKeyGenerator.GenerateObjectKey(context.Now, ext)}";
        await using var stream = file.OpenReadStream();
        var stored = await _storage.StoreAsync(stream, objectKey, ext.ToLowerInvariant(), policy.MaxSizeBytes, cancellationToken);
        if (stored.SizeBytes != request.SizeBytes || !string.Equals(stored.Sha256, sha256, StringComparison.OrdinalIgnoreCase) || !string.Equals(stored.MimeType, mimeType, StringComparison.OrdinalIgnoreCase))
        {
            await _storage.DeleteAsync(objectKey, cancellationToken);
            throw Validation("Avatar content does not match declared metadata.");
        }

        try
        {
            await _repository.UpdateAvatarAsync(new AccountAvatarUpdateRecord(context.UserId, objectKey, stored.MimeType, ext.ToLowerInvariant(), context.Now), cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(objectKey, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarObjectKey))
        {
            await _storage.DeleteAsync(user.AvatarObjectKey, cancellationToken);
        }

        await AuditAsync(context, "avatar-update", "success", "Account avatar updated.", cancellationToken);
        await SecurityEventAsync(context, "auth.account_avatar_updated", "info", "Account avatar updated.", cancellationToken);
        return new AccountAvatarResponse(AvatarUrl());
    }

    public async Task<FileDownloadPayload> GetAvatarAsync(AccountRequestContext context, CancellationToken cancellationToken)
    {
        var user = await RequiredUserAsync(context.UserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user.AvatarObjectKey) || string.IsNullOrWhiteSpace(user.AvatarMimeType))
        {
            throw new DomainException(ApiCodes.NotFound, "Avatar was not found.");
        }

        return new FileDownloadPayload(await _storage.OpenReadAsync(user.AvatarObjectKey, cancellationToken), user.AvatarMimeType, $"avatar{user.AvatarFileExt}", 0, true);
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

    private static string NormalizeSha256(string? value)
    {
        var normalized = NormalizeRequired(value, "sha256", 64).ToLowerInvariant();
        return normalized.Length == 64 && normalized.All(Uri.IsHexDigit) ? normalized : throw Validation("sha256 must be 64 hex characters.");
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
