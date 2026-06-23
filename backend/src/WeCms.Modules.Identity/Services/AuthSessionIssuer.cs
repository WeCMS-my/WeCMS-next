using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Shared.Data;

namespace WeCms.Modules.Identity.Services;

public interface IAuthSessionIssuer
{
    Task<AuthSessionResult> IssueAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        AuthSessionAudit audit,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<AuthSessionResult> IssueInCurrentTransactionAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        AuthSessionAudit audit,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record AuthSessionAudit(
    string Action,
    string Detail,
    string RequestPath);

public sealed class AuthSessionIssuer : IAuthSessionIssuer
{
    private const string AuditResultSuccess = "success";
    private const string AuthAuditModule = "auth";
    private const string AuthAuditResource = "auth";

    private readonly IAuthRepository _repository;
    private readonly IAccessProfileService _accessProfileService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoginFailureLimiter _loginFailureLimiter;
    private readonly IAuthClock _clock;

    public AuthSessionIssuer(
        IAuthRepository repository,
        IAccessProfileService accessProfileService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        ILoginFailureLimiter loginFailureLimiter,
        IAuthClock clock)
    {
        _repository = repository;
        _accessProfileService = accessProfileService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
        _loginFailureLimiter = loginFailureLimiter;
        _clock = clock;
    }

    public async Task<AuthSessionResult> IssueAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        AuthSessionAudit audit,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var session = await IssueInCurrentTransactionAsync(user, requestContext, audit, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return session;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthSessionResult> IssueInCurrentTransactionAsync(
        AuthUserRecord user,
        AuthRequestContext requestContext,
        AuthSessionAudit audit,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessToken = _accessTokenService.Issue(user, now);
        var refreshToken = _refreshTokenService.Issue(now);
        await _repository.CompleteSuccessfulLoginAsync(
            new SuccessfulLoginRecord(
                user.Id,
                requestContext.Ip,
                refreshToken.Hash,
                refreshToken.FamilyId,
                refreshToken.ExpiresAt,
                now),
            cancellationToken);
        await _repository.RecordAuditLogAsync(
            new AuditLogRecord(
                user.Id,
                user.Username,
                AuthAuditModule,
                AuthAuditResource,
                audit.Action,
                user.Username,
                "POST",
                audit.RequestPath,
                requestContext.Ip,
                requestContext.UserAgent,
                requestContext.TraceId,
                AuditResultSuccess,
                audit.Detail,
                _clock.UtcNow),
            cancellationToken);
        await _loginFailureLimiter.ResetAsync(user.Username, requestContext.Ip, cancellationToken);

        var accessProfile = await _accessProfileService.GetAsync(user.Id, cancellationToken);
        var menus = AuthAccessProfileMapper.ToAuthMenuTree(accessProfile.Menus);

        return BuildResult(user, accessToken, refreshToken, accessProfile.PermissionVersion, accessProfile.Roles, accessProfile.Permissions, menus, now);
    }

    private static AuthSessionResult BuildResult(
        AuthUserRecord user,
        IssuedAccessToken accessToken,
        IssuedRefreshToken refreshToken,
        long permissionVersion,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        IReadOnlyList<AuthMenuTreeDto> menus,
        DateTimeOffset now)
    {
        return new AuthSessionResult(
            new LoginResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                ToDto(user),
                permissionVersion,
                roles,
                permissions,
                menus),
            refreshToken.Token,
            refreshToken.ExpiresAt,
            refreshToken.ExpiresAt - now);
    }

    private static AuthUserDto ToDto(AuthUserRecord user)
    {
        return new AuthUserDto(user.Id, user.Username, user.DisplayName);
    }

}
