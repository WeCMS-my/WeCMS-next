using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public sealed class AccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "WeCmsAccessToken";

    private readonly IAccessTokenService _accessTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IAuthClock _clock;

    public AccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAccessTokenService accessTokenService,
        IAuthRepository authRepository,
        IAuthClock clock)
        : base(options, logger, encoder)
    {
        _accessTokenService = accessTokenService;
        _authRepository = authRepository;
        _clock = clock;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = header["Bearer ".Length..].Trim();
        return HandleBearerTokenAsync(token);
    }

    private async Task<AuthenticateResult> HandleBearerTokenAsync(string token)
    {
        var principal = _accessTokenService.Validate(token, _clock.UtcNow);
        if (principal is null)
        {
            return AuthenticateResult.Fail("Invalid access token.");
        }

        var user = await _authRepository.FindUserByIdAsync(principal.UserId, Context.RequestAborted);
        if (user is null
            || !string.Equals(user.Status, "enabled", StringComparison.Ordinal)
            || !string.Equals(user.SecurityStamp, principal.SecurityStamp, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("Authentication is required.");
        }

        Response.Headers["X-Permission-Version"] = user.PermissionVersion.ToString(global::System.Globalization.CultureInfo.InvariantCulture);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, principal.UserId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, principal.Username)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return WriteErrorAsync(Context, ApiCodes.Unauthorized, "Authentication is required.");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        return WriteErrorAsync(Context, ApiCodes.Forbidden, "Permission denied.");
    }

    private static async Task WriteErrorAsync(HttpContext context, int code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = ApiCodes.ToHttpStatus(code);
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object>.Error(code, message, context.TraceIdentifier);
        await using var writer = new Utf8JsonWriter(context.Response.Body);
        writer.WriteStartObject();
        writer.WriteNumber("code", result.Code);
        writer.WriteString("msg", result.Msg);
        writer.WriteNull("data");
        writer.WriteString("traceId", result.TraceId);
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);
    }
}
