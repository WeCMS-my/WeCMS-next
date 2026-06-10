using System.Data.Common;
using Dapper;
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

    Task<IReadOnlyList<string>> GetUserRoleCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(
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
    int PermissionVersion);

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

public sealed class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private async Task<T> ExecuteWithConnectionAsync<T>(
        IDbTransactionFacade? transaction,
        Func<DbConnection, DbTransaction?, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            return await action(transaction.Connection, transaction.Inner, cancellationToken);
        }

        await using var connection = await _connectionFactory.OpenAsync(cancellationToken);
        return await action(connection, null, cancellationToken);
    }

    public async Task<UserRow?> GetUserByUsernameAsync(
        IDbTransactionFacade? transaction,
        string username,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition("""
                SELECT id, username, display_name, password_hash, status,
                       security_stamp, permission_version
                FROM sys_user
                WHERE username = @username
                  AND deleted_at IS NULL
                LIMIT 1
                """,
                new { username },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<UserRow?> GetUserByIdAsync(
        IDbTransactionFacade? transaction,
        long id,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition("""
                SELECT id, username, display_name, password_hash, status,
                       security_stamp, permission_version
                FROM sys_user
                WHERE id = @id
                  AND deleted_at IS NULL
                LIMIT 1
                """,
                new { id },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<long> InsertRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        RefreshTokenInsertRow row,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleAsync<long>(new CommandDefinition("""
                INSERT INTO sys_refresh_token
                    (user_id, token_hash, family_id, expires_at, created_ip, user_agent)
                VALUES
                    (@UserId, @TokenHash, @FamilyId, @ExpiresAt, @CreatedIp, @UserAgent);
                SELECT LAST_INSERT_ID();
                """,
                row,
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<RefreshTokenRow?> GetRefreshTokenByHashAsync(
        IDbTransactionFacade? transaction,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleOrDefaultAsync<RefreshTokenRow>(new CommandDefinition("""
                SELECT id, user_id, token_hash, family_id, expires_at,
                       revoked_at, replaced_by_token_id
                FROM sys_refresh_token
                WHERE token_hash = @tokenHash
                LIMIT 1
                """,
                new { tokenHash },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<int> RevokeRefreshTokenAsync(
        IDbTransactionFacade? transaction,
        long tokenId,
        DateTimeOffset revokedAt,
        long? replacedByTokenId,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE sys_refresh_token
                SET revoked_at = @revokedAt,
                    replaced_by_token_id = @replacedByTokenId
                WHERE id = @tokenId
                  AND revoked_at IS NULL
                """,
                new { tokenId, revokedAt = revokedAt.UtcDateTime, replacedByTokenId },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<int> RevokeRefreshTokenFamilyAsync(
        IDbTransactionFacade? transaction,
        string familyId,
        long exceptTokenId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE sys_refresh_token
                SET revoked_at = @revokedAt
                WHERE family_id = @familyId
                  AND id <> @exceptTokenId
                  AND revoked_at IS NULL
                """,
                new { familyId, exceptTokenId, revokedAt = revokedAt.UtcDateTime },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<long> InsertLoginLogAsync(
        IDbTransactionFacade? transaction,
        LoginLogInsertRow row,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleAsync<long>(new CommandDefinition("""
                INSERT INTO sys_login_log
                    (user_id, username, ip_address, user_agent, result, fail_reason)
                VALUES
                    (@UserId, @Username, @IpAddress, @UserAgent, @Result, @FailReason);
                SELECT LAST_INSERT_ID();
                """,
                row,
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<long> InsertSecurityEventAsync(
        IDbTransactionFacade? transaction,
        SecurityEventInsertRow row,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.QuerySingleAsync<long>(new CommandDefinition("""
                INSERT INTO sys_security_event
                    (user_id, event_type, description, ip_address, user_agent, severity)
                VALUES
                    (@UserId, @EventType, @Description, @IpAddress, @UserAgent, @Severity);
                SELECT LAST_INSERT_ID();
                """,
                row,
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserRoleCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
        {
            var results = await db.QueryAsync<string>(new CommandDefinition("""
                SELECT r.code
                FROM sys_user_role ur
                INNER JOIN sys_role r ON r.id = ur.role_id
                    AND r.status = 1
                    AND r.deleted_at IS NULL
                WHERE ur.user_id = @userId
                """,
                new { userId },
                transaction: tx,
                cancellationToken: ct));
            return results.AsList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(
        IDbTransactionFacade? transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
        {
            var results = await db.QueryAsync<string>(new CommandDefinition("""
                SELECT p.code
                FROM sys_user_role ur
                INNER JOIN sys_role_permission rp ON rp.role_id = ur.role_id
                INNER JOIN sys_permission p ON p.id = rp.permission_id
                    AND p.status = 1
                    AND p.deleted_at IS NULL
                WHERE ur.user_id = @userId
                """,
                new { userId },
                transaction: tx,
                cancellationToken: ct));
            return results.AsList();
        }, cancellationToken);
    }

    public async Task<int> UpdateUserLastLoginAsync(
        IDbTransactionFacade? transaction,
        long userId,
        DateTimeOffset loginAt,
        string ip,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithConnectionAsync(transaction, async (db, tx, ct) =>
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE sys_user
                SET last_login_at = @loginAt,
                    last_login_ip = @ip
                WHERE id = @userId
                """,
                new { userId, loginAt = loginAt.UtcDateTime, ip },
                transaction: tx,
                cancellationToken: ct)), cancellationToken);
    }
}
