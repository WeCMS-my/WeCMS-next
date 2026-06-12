using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using WeCms.Api.Security;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Auth;

public sealed class AccessTokenValidationEventsTests
{
    [Fact]
    public async Task TokenValidated_ShouldFail_WhenSecurityStampIsStale()
    {
        var validator = new FakeAccessTokenStateValidator(false);
        var events = new AccessTokenValidationEvents(validator);
        var context = CreateContext(
            userId: "1",
            permissionVersion: "3",
            securityStamp: "jwt-stamp");

        await events.TokenValidated(context);

        Assert.NotNull(context.Result);
        Assert.False(context.Result.Succeeded);
        Assert.Equal(1, validator.Calls);
    }

    [Fact]
    public async Task TokenValidated_ShouldFail_WhenPermissionVersionIsStale()
    {
        var validator = new FakeAccessTokenStateValidator(false);
        var events = new AccessTokenValidationEvents(validator);
        var context = CreateContext(
            userId: "1",
            permissionVersion: "2",
            securityStamp: "stamp");

        await events.TokenValidated(context);

        Assert.NotNull(context.Result);
        Assert.False(context.Result.Succeeded);
        Assert.Equal(1, validator.Calls);
    }

    [Fact]
    public async Task TokenValidated_ShouldSucceed_WhenTokenStateMatches()
    {
        var validator = new FakeAccessTokenStateValidator(true);
        var events = new AccessTokenValidationEvents(validator);
        var context = CreateContext(
            userId: "1",
            permissionVersion: "3",
            securityStamp: "stamp");

        await events.TokenValidated(context);

        Assert.Null(context.Result);
        Assert.Equal(1, validator.Calls);
    }

    private static TokenValidatedContext CreateContext(
        string userId,
        string permissionVersion,
        string securityStamp)
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("permissionVersion", permissionVersion),
            new Claim("securityStamp", securityStamp),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = principal
        };
    }

    private sealed class FakeAccessTokenStateValidator : IAccessTokenStateValidator
    {
        private readonly bool _isValid;

        public FakeAccessTokenStateValidator(bool isValid)
        {
            _isValid = isValid;
        }

        public int Calls { get; private set; }

        public Task<bool> ValidateAsync(
            AccessTokenState tokenState,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_isValid);
        }
    }
}
