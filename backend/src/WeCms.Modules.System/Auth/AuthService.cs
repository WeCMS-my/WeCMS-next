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

    Task<CaptchaChallengeResponse> CreateCaptchaAsync(CancellationToken cancellationToken);

    Task<RefreshResponse> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string refreshToken,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken);

    Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(
        VerifyTwoFactorRequest request,
        string ipAddress,
        string userAgent,
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
    private readonly IAuthRiskService _authRiskService;
    private readonly ICaptchaService _captchaService;
    private readonly ITwoFactorLoginService _twoFactorLoginService;
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
        IAuthRiskService authRiskService,
        ICaptchaService captchaService,
        ITwoFactorLoginService twoFactorLoginService,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _tokenGenerator = tokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _authRiskService = authRiskService;
        _captchaService = captchaService;
        _twoFactorLoginService = twoFactorLoginService;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var riskDecision = await _authRiskService.EvaluateLoginAsync(
            request.Username,
            ipAddress,
            cancellationToken);
        if (riskDecision.RequiresCaptcha)
        {
            if (string.IsNullOrWhiteSpace(request.CaptchaChallengeId) ||
                string.IsNullOrWhiteSpace(request.CaptchaCode) ||
                !await _captchaService.VerifyAsync(request.CaptchaChallengeId, request.CaptchaCode, cancellationToken))
            {
                await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
                    null, request.Username, ipAddress, userAgent, 0, "验证码校验失败"), cancellationToken);
                await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                    null, "login_captcha_failed", "登录验证码校验失败", ipAddress, userAgent, 2), cancellationToken);
                throw new DomainException(ApiCodes.ValidationError, "验证码无效或已过期");
            }
        }

        if (riskDecision.IsBlocked)
        {
            await _repository.InsertLoginLogAsync(null, new LoginLogInsertRow(
                null, request.Username, ipAddress, userAgent, 0, "登录限流"), cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                null, riskDecision.EventType, riskDecision.Description, ipAddress, userAgent, riskDecision.Severity), cancellationToken);
            throw new DomainException(ApiCodes.TooManyRequests, "登录失败过多，请稍后再试");
        }

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

        if (user.TwoFactorEnabled)
        {
            var challenge = await _twoFactorLoginService.CreateChallengeAsync(user.Id, user.Username, cancellationToken);
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                user.Id,
                "two_factor_login_required",
                $"用户需要二次验证：{user.Username}",
                ipAddress,
                userAgent,
                1), cancellationToken);
            return new LoginResponse(null, null, 0, true, challenge.ChallengeId, challenge.Method);
        }

        return await IssueLoginTokensAsync(user, request.Username, ipAddress, userAgent, cancellationToken);
    }

    public async Task<CaptchaChallengeResponse> CreateCaptchaAsync(CancellationToken cancellationToken)
    {
        var challenge = await _captchaService.CreateChallengeAsync(cancellationToken);
        return new CaptchaChallengeResponse(challenge.ChallengeId, challenge.ImageData, challenge.ExpiresIn);
    }

    public async Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(
        VerifyTwoFactorRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var verification = await _twoFactorLoginService.VerifyChallengeAsync(
            request.ChallengeId,
            request.Code,
            cancellationToken);
        if (!verification.IsValid)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                null,
                "two_factor_login_failed",
                "二次验证失败",
                ipAddress,
                userAgent,
                2), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "二次验证码无效或已过期");
        }

        var user = await _repository.GetUserByIdAsync(null, verification.UserId, cancellationToken);
        if (user is null || user.Status != 1 || !user.TwoFactorEnabled)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                verification.UserId,
                "two_factor_login_user_invalid",
                "二次验证用户不存在、禁用或未启用二次验证",
                ipAddress,
                userAgent,
                2), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "用户已禁用");
        }

        var login = await IssueLoginTokensAsync(user, user.Username, ipAddress, userAgent, cancellationToken);
        return new VerifyTwoFactorResponse(login.AccessToken!, login.RefreshToken!, login.ExpiresIn);
    }

    private async Task<LoginResponse> IssueLoginTokensAsync(
        UserRow user,
        string username,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
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
                new LoginLogInsertRow(user.Id, username, ipAddress, userAgent, 1, ""),
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

            if (txToken.RevokedAt is not null)
            {
                await _repository.RevokeRefreshTokenFamilyAsync(transaction, txToken.FamilyId, 0, _clock.UtcNow, cancellationToken);
                var tokenReuseSeverity = await _authRiskService.GetRefreshTokenReuseSeverityAsync(
                    txToken.UserId,
                    ipAddress,
                    cancellationToken);
                await _repository.InsertSecurityEventAsync(transaction, new SecurityEventInsertRow(
                    txToken.UserId,
                    "token_reuse",
                    $"已吊销 token 被复用，撤销整个 family：{txToken.FamilyId}",
                    ipAddress,
                    userAgent,
                    tokenReuseSeverity),
                    cancellationToken);
                throw new DomainException(ApiCodes.Unauthorized, "令牌已被吊销");
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
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var storedToken = await _repository.GetRefreshTokenByHashAsync(null, tokenHash, cancellationToken);

        if (storedToken is null)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                null,
                "logout_refresh_invalid",
                "登出请求使用不存在的 refresh token",
                ipAddress,
                userAgent,
                1), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "刷新令牌无效或已失效");
        }

        if (storedToken.RevokedAt is not null)
        {
            await _repository.InsertSecurityEventAsync(null, new SecurityEventInsertRow(
                storedToken.UserId,
                "logout_refresh_revoked",
                "登出请求复用已吊销的 refresh token",
                ipAddress,
                userAgent,
                2), cancellationToken);
            throw new DomainException(ApiCodes.Unauthorized, "刷新令牌无效或已失效");
        }

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
        var menus = await _repository.GetUserMenusAsync(null, userId, cancellationToken);

        return new CurrentUserResponse(
            new MeUserInfo(user.Id, user.Username, user.DisplayName),
            roles,
            permissions,
            BuildMenuTree(menus));
    }

    private static IReadOnlyList<CurrentUserMenuDto> BuildMenuTree(IReadOnlyList<CurrentUserMenuRow> rows)
    {
        var byParent = rows.ToLookup(row => row.ParentId);
        var knownIds = rows.Select(row => row.Id).ToHashSet();
        var visiting = new HashSet<long>();

        foreach (var row in rows)
        {
            if (row.ParentId is not null && !knownIds.Contains(row.ParentId.Value))
                throw new DomainException(ApiCodes.SystemError, "用户菜单树数据不完整");
        }

        return BuildChildren(null);

        IReadOnlyList<CurrentUserMenuDto> BuildChildren(long? parentId)
        {
            var children = byParent[parentId]
                .OrderBy(row => row.SortOrder)
                .ThenBy(row => row.Id)
                .ToArray();
            if (children.Length == 0)
                return Array.Empty<CurrentUserMenuDto>();

            var result = new List<CurrentUserMenuDto>(children.Length);
            foreach (var child in children)
            {
                if (!visiting.Add(child.Id))
                    throw new DomainException(ApiCodes.SystemError, "用户菜单树存在循环引用");

                result.Add(new CurrentUserMenuDto(
                    child.Id,
                    child.Code,
                    child.Name,
                    child.Component,
                    child.RoutePath,
                    BuildChildren(child.Id)));

                visiting.Remove(child.Id);
            }

            return result;
        }
    }
}
