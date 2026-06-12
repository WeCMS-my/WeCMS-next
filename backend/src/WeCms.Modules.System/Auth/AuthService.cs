using WeCms.Shared;
using WeCms.Shared.Security;
using WeCms.Shared.Data;
using WeCms.Shared.Id;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private const int AccessTokenExpirySeconds = 1800;
    private const int RefreshTokenExpiryDays = 7;

    public AuthService(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITokenGenerator tokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _tokenGenerator = tokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
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
        var familyId = _idGenerator.NewGuid().ToString("D");
        var expiresAt = _clock.UtcNow.AddDays(RefreshTokenExpiryDays);

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var transaction = _unitOfWork.Transaction;
            var refreshTokenId = await _repository.InsertRefreshTokenAsync(
                transaction,
                new RefreshTokenInsertRow(
                    user.Id,
                    refreshTokenHash,
                    familyId,
                    expiresAt,
                    ipAddress,
                    userAgent),
                cancellationToken);

            if (refreshTokenId <= 0)
            {
                throw new DomainException(ApiCodes.SystemError, "登录会话创建失败");
            }

            var updatedRows = await _repository.UpdateUserLastLoginAsync(
                transaction,
                user.Id,
                _clock.UtcNow,
                ipAddress,
                cancellationToken);

            if (updatedRows != 1)
            {
                throw new DomainException(ApiCodes.SystemError, "更新登录状态失败");
            }

            var loginLogId = await _repository.InsertLoginLogAsync(
                transaction,
                new LoginLogInsertRow(user.Id, request.Username, ipAddress, userAgent, 1, ""),
                cancellationToken);

            if (loginLogId <= 0)
            {
                throw new DomainException(ApiCodes.SystemError, "登录日志写入失败");
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return new LoginResponse(accessToken, refreshToken, AccessTokenExpirySeconds);
        }
        catch (DomainException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw new DomainException(ApiCodes.SystemError, "登录事务执行失败", ex);
        }
    }

    public async Task<RefreshResponse> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(request.RefreshToken);

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var transaction = _unitOfWork.Transaction;
            var txToken = await _repository.GetRefreshTokenByHashAsync(transaction, tokenHash, cancellationToken);
            if (txToken is null)
            {
                await _repository.InsertSecurityEventAsync(
                    transaction,
                    new SecurityEventInsertRow(
                        null,
                        "refresh_invalid",
                        "无效的 refresh token",
                        ipAddress,
                        userAgent,
                        1),
                    cancellationToken);
                throw new DomainException(ApiCodes.Unauthorized, "无效的刷新令牌");
            }

            var user = await _repository.GetUserByIdAsync(transaction, txToken.UserId, cancellationToken);
            if (user is null || user.Status != 1)
            {
                await _repository.InsertSecurityEventAsync(
                    transaction,
                    new SecurityEventInsertRow(
                        txToken.UserId,
                        "refresh_user_disabled",
                        "用户不存在或已禁用",
                        ipAddress,
                        userAgent,
                        2),
                    cancellationToken);
                throw new DomainException(ApiCodes.Unauthorized, "用户已禁用");
            }

            if (txToken.RevokedAt is not null)
            {
                await _repository.RevokeRefreshTokenFamilyAsync(transaction, txToken.FamilyId, 0, _clock.UtcNow, cancellationToken);
                await _repository.InsertSecurityEventAsync(transaction, new SecurityEventInsertRow(
                    txToken.UserId,
                    "token_reuse",
                    $"已吊销 token 被复用，撤销整个 family：{txToken.FamilyId}",
                    ipAddress,
                    userAgent,
                    3),
                    cancellationToken);
                throw new DomainException(ApiCodes.Unauthorized, "令牌已被吊销");
            }

            if (txToken.ExpiresAt < _clock.UtcNow)
            {
                await _repository.InsertSecurityEventAsync(
                    transaction,
                    new SecurityEventInsertRow(
                        txToken.UserId,
                        "refresh_expired",
                        "refresh token 已过期",
                        ipAddress,
                        userAgent,
                        1),
                    cancellationToken);
                throw new DomainException(ApiCodes.Unauthorized, "刷新令牌已过期");
            }

            var currentUser = new CurrentUser(
                txToken.UserId,
                user.Username,
                user.DisplayName,
                user.PermissionVersion,
                user.SecurityStamp);
            var newAccessToken = _tokenService.GenerateAccessToken(currentUser);
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
            var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshToken);
            var expiresAt = _clock.UtcNow.AddDays(RefreshTokenExpiryDays);

            var newTokenId = await _repository.InsertRefreshTokenAsync(
                transaction,
                new RefreshTokenInsertRow(txToken.UserId, newRefreshTokenHash, txToken.FamilyId, expiresAt, ipAddress, userAgent),
                cancellationToken);

            if (newTokenId <= 0)
            {
                throw new DomainException(ApiCodes.SystemError, "刷新令牌写入失败");
            }

            var revokedRows = await _repository.RevokeRefreshTokenAsync(
                transaction,
                txToken.Id,
                _clock.UtcNow,
                newTokenId,
                cancellationToken);

            if (revokedRows != 1)
            {
                throw new DomainException(ApiCodes.Unauthorized, "令牌已失效");
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return new RefreshResponse(newAccessToken, newRefreshToken, AccessTokenExpirySeconds);
        }
        catch (DomainException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw new DomainException(ApiCodes.SystemError, "刷新事务执行失败", ex);
        }
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var storedToken = await _repository.GetRefreshTokenByHashAsync(null, tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
            return;

        var revokedRows = await _repository.RevokeRefreshTokenAsync(
            null,
            storedToken.Id,
            _clock.UtcNow,
            null,
            cancellationToken);

        if (revokedRows != 1)
        {
            throw new DomainException(ApiCodes.SystemError, "登出令牌吊销失败");
        }
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
            Array.Empty<CurrentUserMenuDto>());
    }
}
