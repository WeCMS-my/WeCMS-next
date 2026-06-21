namespace WeCms.Modules.Identity.Contracts;

public sealed record AccountProfileResponse(
    long Id,
    string Username,
    string DisplayName,
    string? Email,
    string? Phone,
    string? AvatarUrl);

public sealed record UpdateAccountProfileRequest(string DisplayName, string? Email, string? Phone);

public sealed record ChangeAccountPasswordRequest(string OldPassword, string NewPassword);

public sealed record AccountAvatarUploadRequest(string OriginalName, string MimeType, long SizeBytes, string Sha256);

public sealed record AccountAvatarResponse(string AvatarUrl);

public sealed record AccountSecurityResponse(
    bool TwoFactorEnabled,
    bool TwoFactorResetRequired,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    string? LastLoginIp);
