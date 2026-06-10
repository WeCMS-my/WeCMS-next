using WeCms.Infrastructure.Data;
using WeCms.Infrastructure.Security;
using WeCms.Shared;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Modules.System.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken);

    Task<RefreshResponse> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<CurrentUserResponse> GetCurrentUserAsync(
        long userId,
        CancellationToken cancellationToken);
}

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IClock _clock;
    private const int AccessTokenExpirySeconds = 1800;
    private const int RefreshTokenExpiryDays = 7;

    public AuthService(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITokenGenerator tokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IClock clock)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _tokenGenerator = tokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _clock = clock;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByUsernameAsync(null, request.Username, cancellationToken);

        if (user is null)
        {
            await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
                null, request.Username, ipAddress, userAgent, 0, "用户名或密码错误"), cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                null, "login_failed", $"未知用户尝试登录：{request.Username}", ipAddress, userAgent, 1), cancellationToken);
            throw new DomainException(ApiCodes.BusinessError, "用户名或密码错误");
        }

        if (user.Status != 1)
        {
            await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
                user.Id, request.Username, ipAddress, userAgent, 0, "用户已禁用"), cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                user.Id, "login_failed_disabled", $"已禁用用户尝试登录：{request.Username}", ipAddress, userAgent, 2), cancellationToken);
            throw new DomainException(ApiCodes.BusinessError, "用户名或密码错误");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
                user.Id, request.Username, ipAddress, userAgent, 0, "密码错误"), cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                user.Id, "login_failed_password", $"密码错误：{request.Username}", ipAddress, userAgent, 1), cancellationToken);
            throw new DomainException(ApiCodes.BusinessError, "用户名或密码错误");
        }

        var currentUser = new CurrentUser(user.Id, user.Username, user.DisplayName, user.PermissionVersion, user.SecurityStamp);
        var accessToken = _tokenService.GenerateAccessToken(currentUser);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var familyId = Guid.NewGuid().ToString("D");
        var expiresAt = _clock.UtcNow.AddDays(RefreshTokenExpiryDays);

        await _repository.InsertRefreshTokenAsync(null, new RefreshTokenInsertRow(
            user.Id, refreshTokenHash, familyId, expiresAt, ipAddress, userAgent), cancellationToken);

        await _repository.UpdateUserLastLoginAsync(null, user.Id, _clock.UtcNow, ipAddress, cancellationToken);
        await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
            user.Id, request.Username, ipAddress, userAgent, 1, ""), cancellationToken);

        return new LoginResponse(accessToken, refreshToken, AccessTokenExpirySeconds);
    }

    public async Task<RefreshResponse> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        var storedToken = await _repository.GetRefreshTokenByHashAsync(null, tokenHash, cancellationToken);

        if (storedToken is null)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                null, "refresh_invalid", "无效的 refresh token", ipAddress, userAgent, 1), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "无效的刷新令牌");
        }

        if (storedToken.RevokedAt is not null)
        {
            await _repository.RevokeRefreshTokenFamilyAsync(
                null, storedToken.FamilyId, 0, _clock.UtcNow, cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                storedToken.UserId, "token_reuse", $"已吊销 token 被复用，撤销整个 family：{storedToken.FamilyId}",
                ipAddress, userAgent, 3), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "令牌已被吊销");
        }

        if (storedToken.ExpiresAt < _clock.UtcNow)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                storedToken.UserId, "refresh_expired", "refresh token 已过期", ipAddress, userAgent, 1), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "刷新令牌已过期");
        }

        var user = await _repository.GetUserByIdAsync(null, storedToken.UserId, cancellationToken);
        if (user is null || user.Status != 1)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                storedToken.UserId, "refresh_user_disabled", "用户不存在或已禁用", ipAddress, userAgent, 2), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "用户已禁用");
        }

        var currentUser = new CurrentUser(user.Id, user.Username, user.DisplayName, user.PermissionVersion, user.SecurityStamp);
        var newAccessToken = _tokenService.GenerateAccessToken(currentUser);
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshToken);
        var expiresAt = _clock.UtcNow.AddDays(RefreshTokenExpiryDays);

        var newTokenId = await _repository.InsertRefreshTokenAsync(null, new RefreshTokenInsertRow(
            user.Id, newRefreshTokenHash, storedToken.FamilyId, expiresAt, ipAddress, userAgent), cancellationToken);

        await _repository.RevokeRefreshTokenAsync(null, storedToken.Id, _clock.UtcNow, newTokenId, cancellationToken);

        return new RefreshResponse(newAccessToken, newRefreshToken, AccessTokenExpirySeconds);
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var storedToken = await _repository.GetRefreshTokenByHashAsync(null, tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
            return;

        await _repository.RevokeRefreshTokenAsync(null, storedToken.Id, _clock.UtcNow, null, cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(null, userId, cancellationToken);

        if (user is null || user.Status != 1)
            throw new DomainException(ApiCodes.Unauthorized, "用户不存在或已禁用");

        var roles = await _repository.GetUserRoleCodesAsync(null, userId, cancellationToken);
        var permissions = await _repository.GetUserPermissionCodesAsync(null, userId, cancellationToken);

        return new CurrentUserResponse(
            new MeUserInfo(user.Id, user.Username, user.DisplayName),
            roles,
            permissions,
            Array.Empty<object>());
    }
}
