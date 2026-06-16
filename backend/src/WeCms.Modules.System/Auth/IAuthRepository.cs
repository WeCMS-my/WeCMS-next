namespace WeCms.Modules.System.Auth;

public interface IAuthRepository
{
    Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken);

    Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken);

    Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken);

    Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken);

    Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken);

    Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}
