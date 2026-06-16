using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Modules.System.Auth;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string EnabledStatus = "enabled";

    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IAuthClock clock,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestContext);

        var username = NormalizeRequired(request.Username, nameof(request.Username));
        var password = NormalizeRequired(request.Password, nameof(request.Password));
        var user = await _repository.FindUserByUsernameAsync(username, cancellationToken);
        if (user is null
            || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal)
            || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(username, user?.Id, requestContext, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, InvalidCredentialsMessage);
        }

        var now = _clock.UtcNow;
        var accessToken = _accessTokenService.Issue(user, now);
        var refreshToken = _refreshTokenService.Issue(now);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.CompleteSuccessfulLoginAsync(
                new SuccessfulLoginRecord(
                    user.Id,
                    requestContext.Ip,
                    refreshToken.Hash,
                    refreshToken.FamilyId,
                    refreshToken.ExpiresAt,
                    now),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var roles = await _repository.ListRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(user.Id, cancellationToken);

        return new LoginResponse(
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresAt,
            ToDto(user),
            roles,
            permissions,
            []);
    }

    public async Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestContext);

        var refreshTokenValue = NormalizeRequired(request.RefreshToken, nameof(request.RefreshToken));
        var refreshTokenHash = _refreshTokenService.Hash(refreshTokenValue);
        var existingToken = await _repository.FindRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);
        if (existingToken is null)
        {
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        var now = _clock.UtcNow;
        if (existingToken.RevokedAt is not null)
        {
            await RecordRefreshReuseAsync(existingToken, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        if (existingToken.ExpiresAt <= now)
        {
            await RecordRefreshSecurityEventAsync(existingToken, requestContext, "auth.refresh_expired", "Expired refresh token rejected.", now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        if (!string.Equals(existingToken.UserStatus, EnabledStatus, StringComparison.Ordinal))
        {
            await RecordRefreshSecurityEventAsync(existingToken, requestContext, "auth.refresh_user_disabled", "Disabled user refresh rejected.", now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        var user = new AuthUserRecord(
            existingToken.UserId,
            existingToken.Username,
            existingToken.DisplayName,
            PasswordHash: string.Empty,
            existingToken.UserStatus,
            existingToken.IsSuperAdmin);
        var accessToken = _accessTokenService.Issue(user, now);
        var newRefreshToken = _refreshTokenService.Issue(now) with { FamilyId = existingToken.FamilyId };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.CompleteRefreshRotationAsync(
                new RefreshRotationRecord(
                    existingToken.Id,
                    existingToken.UserId,
                    newRefreshToken.Hash,
                    existingToken.FamilyId,
                    newRefreshToken.ExpiresAt,
                    now),
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (RefreshTokenAlreadyRevokedException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecordRefreshReuseAsync(existingToken, requestContext, now, cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var roles = await _repository.ListRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(user.Id, cancellationToken);

        return new LoginResponse(
            accessToken.Token,
            newRefreshToken.Token,
            accessToken.ExpiresAt,
            ToDto(user),
            roles,
            permissions,
            []);
    }

    public async Task<AuthMeResponse> MeAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _repository.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        var roles = await _repository.ListRoleCodesAsync(user.Id, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(user.Id, cancellationToken);

        return new AuthMeResponse(ToDto(user), roles, permissions, []);
    }

    private async Task RecordFailedLoginAsync(
        string username,
        long? userId,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _repository.RecordFailedLoginAsync(
            new FailedLoginRecord(username, requestContext.Ip, requestContext.UserAgent, "invalid_credentials", now),
            cancellationToken);
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                "auth.login_failed",
                userId,
                username,
                requestContext.Ip,
                "warning",
                InvalidCredentialsMessage,
                now),
            cancellationToken);
    }

    private async Task RecordRefreshSecurityEventAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        string eventType,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                eventType,
                token.UserId,
                token.Username,
                requestContext.Ip,
                "warning",
                message,
                now),
            cancellationToken);
    }

    private async Task RecordRefreshReuseAsync(
        RefreshTokenRecord token,
        AuthRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.RevokeRefreshTokenFamilyAsync(token.FamilyId, now, cancellationToken);
            await RecordRefreshSecurityEventAsync(token, requestContext, "auth.refresh_reuse", "Refresh token reuse detected.", now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }

    private static AuthUserDto ToDto(AuthUserRecord user)
    {
        return new AuthUserDto(user.Id, user.Username, user.DisplayName, user.IsSuperAdmin);
    }
}
