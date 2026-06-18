using Microsoft.AspNetCore.Http;
using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

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

public sealed record AccountRequestContext(long UserId, string Username, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record AccountProfileRecord(
    long Id,
    string Username,
    string DisplayName,
    string PasswordHash,
    string? Email,
    string? Phone,
    string? AvatarObjectKey,
    string? AvatarMimeType,
    string? AvatarFileExt,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    string? LastLoginIp);

public sealed record AccountProfileUpdateRecord(long UserId, string DisplayName, string? Email, string? Phone, DateTimeOffset Now);

public sealed record AccountPasswordUpdateRecord(long UserId, string PasswordHash, DateTimeOffset Now);

public sealed record AccountAvatarUpdateRecord(long UserId, string ObjectKey, string MimeType, string FileExt, DateTimeOffset Now);

public sealed record AccountAuditRecord(long UserId, string Username, string Action, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset CreatedAt);

public sealed record AccountSecurityEventRecord(string EventType, long UserId, string Username, string Ip, string Severity, string Message, DateTimeOffset CreatedAt);

public interface IAccountProfileService
{
    Task<AccountProfileResponse> GetProfileAsync(AccountRequestContext context, CancellationToken cancellationToken);
    Task<AccountProfileResponse> UpdateProfileAsync(UpdateAccountProfileRequest request, AccountRequestContext context, CancellationToken cancellationToken);
    Task ChangePasswordAsync(ChangeAccountPasswordRequest request, AccountRequestContext context, CancellationToken cancellationToken);
    Task<AccountAvatarResponse> UploadAvatarAsync(AccountAvatarUploadRequest request, IFormFile file, AccountRequestContext context, CancellationToken cancellationToken);
    Task<FileDownloadPayload> GetAvatarAsync(AccountRequestContext context, CancellationToken cancellationToken);
    Task<AccountSecurityResponse> GetSecurityAsync(AccountRequestContext context, CancellationToken cancellationToken);
}

public interface IAccountProfileRepository
{
    Task<AccountProfileRecord?> GetAsync(long userId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, long exceptUserId, CancellationToken cancellationToken);
    Task<bool> PhoneExistsAsync(string phone, long exceptUserId, CancellationToken cancellationToken);
    Task UpdateProfileAsync(AccountProfileUpdateRecord record, CancellationToken cancellationToken);
    Task UpdatePasswordAsync(AccountPasswordUpdateRecord record, CancellationToken cancellationToken);
    Task RevokeRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task UpdateAvatarAsync(AccountAvatarUpdateRecord record, CancellationToken cancellationToken);
    Task RecordAuditAsync(AccountAuditRecord record, CancellationToken cancellationToken);
    Task RecordSecurityEventAsync(AccountSecurityEventRecord record, CancellationToken cancellationToken);
}
