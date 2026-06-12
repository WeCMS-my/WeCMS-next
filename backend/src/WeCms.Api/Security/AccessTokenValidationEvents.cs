using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WeCms.Shared.Security;

namespace WeCms.Api.Security;

public sealed class AccessTokenValidationEvents : JwtBearerEvents
{
    private readonly IAccessTokenStateValidator _tokenStateValidator;

    public AccessTokenValidationEvents(IAccessTokenStateValidator tokenStateValidator)
    {
        _tokenStateValidator = tokenStateValidator;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null)
        {
            context.Fail("Token principal is missing.");
            return;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var permissionVersionClaim = principal.FindFirst("permissionVersion")?.Value;
        var securityStamp = principal.FindFirst("securityStamp")?.Value;

        if (!long.TryParse(userIdClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            || !int.TryParse(permissionVersionClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var permissionVersion)
            || string.IsNullOrWhiteSpace(securityStamp))
        {
            context.Fail("Token version claims are invalid.");
            return;
        }

        var isValid = await _tokenStateValidator.ValidateAsync(
            new AccessTokenState(userId, permissionVersion, securityStamp),
            context.HttpContext.RequestAborted);

        if (!isValid)
        {
            context.Fail("Token state is stale.");
        }
    }
}
