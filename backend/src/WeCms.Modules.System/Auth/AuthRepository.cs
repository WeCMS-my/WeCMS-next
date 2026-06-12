using WeCms.Shared.Data;

namespace WeCms.Modules.System.Auth;

public interface IAuthRepository
{
    Task<UserRow?> GetUserByUsernameAsync(
        IDbTransactionFacade? transaction,
        string username,
        CancellationToken cancellationToken);

    Task<UserRow?> GetUserByIdAsync(
        IDbTransactionFacade? transaction,
        long id,
        CancellationToken cancellationToken);

    Task<long> InsertRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        RefreshTokenInsertRow row,
        CancellationToken cancellationToken);

    Task<RefreshTokenRow?> GetRefreshTokenByHashAsync(
        IDbTransactionFacade? transaction,
        string tokenHash,
        CancellationToken cancellationToken);

    Task<int> RevokeRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        long tokenId,
        DateTimeOffset revokedAt,
        long? replacedByTokenId,
        CancellationToken cancellationToken);

    Task<int> RevokeRefreshTokenFamilyAsync(
        IDbTransactionFacade? transaction,
        string familyId,
        long exceptTokenId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken);

    Task<long> InsertLoginLogAsync(
        IDbTransactionFacade? transaction,
        LoginLogInsertRow row,
        CancellationToken cancellationToken);

    Task<long> InsertSecurityEventAsync(
        IDbTransactionFacade? transaction,
        SecurityEventInsertRow row,
        CancellationToken cancellationToken);

    Task<int> CountRecentFailedLoginAttemptsAsync(
        IDbTransactionFacade? transaction,
        string? username,
        string? ipAddress,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    Task<int> CountRecentSecurityEventsAsync(
        IDbTransactionFacade? transaction,
        string eventType,
        long? userId,
        string? ipAddress,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUserRoleCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CurrentUserMenuRow>> GetUserMenusAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken);

    Task<int> UpdateUserLastLoginAsync(
        IDbTransactionFacade? transaction,
        long userId,
        DateTimeOffset loginAt,
        string ip,
        CancellationToken cancellationToken);
}

public sealed record UserRow(
    long Id,
    string Username,
    string DisplayName,
    string PasswordHash,
    int Status,
    string SecurityStamp,
    int PermissionVersion,
    bool TwoFactorEnabled = false);

public sealed record RefreshTokenInsertRow(
    long UserId,
    string TokenHash,
    string FamilyId,
    DateTimeOffset ExpiresAt,
    string CreatedIp,
    string UserAgent);

public sealed record RefreshTokenRow(
    long Id,
    long UserId,
    string TokenHash,
    string FamilyId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    long? ReplacedByTokenId);

public sealed record CurrentUserMenuRow(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    string Component,
    string RoutePath,
    int SortOrder);

public sealed record LoginLogInsertRow(
    long? UserId,
    string Username,
    string IpAddress,
    string UserAgent,
    int Result,
    string FailReason);

public sealed record SecurityEventInsertRow(
    long? UserId,
    string EventType,
    string Description,
    string IpAddress,
    string UserAgent,
    int Severity);
