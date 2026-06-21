namespace WeCms.Modules.Identity.Records;

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

public sealed record AccountSecurityEventRecord(string EventType, long UserId, string Username, string Ip, string Severity, string Message, DateTimeOffset CreatedAt, string TraceId = "");
