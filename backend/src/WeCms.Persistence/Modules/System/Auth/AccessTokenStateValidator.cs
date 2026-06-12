using Dapper;
using WeCms.Shared.Data;
using WeCms.Shared.Security;

namespace WeCms.Persistence.Modules.System.Auth;

public sealed class AccessTokenStateValidator : IAccessTokenStateValidator
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AccessTokenStateValidator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> ValidateAsync(
        AccessTokenState tokenState,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserTokenStateRow>(new CommandDefinition("""
            SELECT id, status, permission_version, security_stamp
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new { userId = tokenState.UserId },
            cancellationToken: cancellationToken));

        return row is not null
            && row.Status == 1
            && row.PermissionVersion == tokenState.PermissionVersion
            && string.Equals(row.SecurityStamp, tokenState.SecurityStamp, StringComparison.Ordinal);
    }

    private sealed record UserTokenStateRow(
        long Id,
        int Status,
        int PermissionVersion,
        string SecurityStamp);
}
