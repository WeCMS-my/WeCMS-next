using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Auth;

public sealed class CookieAuthOriginValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AllowsConfiguredOrigin()
    {
        var repository = new FakeAuthRepository();
        var validator = CreateValidator(repository, new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "true"
        });
        var context = CreateHttpContext(origin: "https://admin.example.com");

        await validator.ValidateAsync(context, CookieAuthOriginEndpoints.Refresh, RequestContext(), CancellationToken.None);

        Assert.Equal(0, repository.SecurityEventCount);
    }

    [Fact]
    public async Task ValidateAsync_AllowsConfiguredRefererFallbackWhenOriginIsMissing()
    {
        var repository = new FakeAuthRepository();
        var validator = CreateValidator(repository, new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "true",
            ["Security:AllowRefererFallbackForCookieAuth"] = "true"
        });
        var context = CreateHttpContext(referer: "https://admin.example.com/settings/security");

        await validator.ValidateAsync(context, CookieAuthOriginEndpoints.Logout, RequestContext(), CancellationToken.None);

        Assert.Equal(0, repository.SecurityEventCount);
    }

    [Fact]
    public async Task ValidateAsync_RejectsIllegalOriginAndWritesSecurityEvent()
    {
        var repository = new FakeAuthRepository();
        var validator = CreateValidator(repository, new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "true"
        });
        var context = CreateHttpContext(origin: "https://evil.example.net");

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => validator.ValidateAsync(context, CookieAuthOriginEndpoints.Refresh, RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Forbidden, exception.Code);
        Assert.Equal("Cookie authenticated request origin is not allowed.", exception.Message);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("auth.cookie_origin_rejected", repository.LastSecurityEventType);
        Assert.Equal("warning", repository.LastSecurityEventSeverity);
    }

    [Fact]
    public async Task ValidateAsync_RejectsMissingOriginWhenRefererFallbackIsDisabled()
    {
        var repository = new FakeAuthRepository();
        var validator = CreateValidator(repository, new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "true",
            ["Security:AllowRefererFallbackForCookieAuth"] = "false"
        });
        var context = CreateHttpContext(referer: "https://admin.example.com/settings/security");

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => validator.ValidateAsync(context, CookieAuthOriginEndpoints.Logout, RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Forbidden, exception.Code);
        Assert.Equal(1, repository.SecurityEventCount);
    }

    [Fact]
    public async Task ValidateAsync_RejectsMissingOriginWithIllegalRefererFallback()
    {
        var repository = new FakeAuthRepository();
        var validator = CreateValidator(repository, new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "true",
            ["Security:AllowRefererFallbackForCookieAuth"] = "true"
        });
        var context = CreateHttpContext(referer: "https://evil.example.net/settings/security");

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => validator.ValidateAsync(context, CookieAuthOriginEndpoints.TwoFactorVerify, RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Forbidden, exception.Code);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Contains(CookieAuthOriginEndpoints.TwoFactorVerify, repository.LastSecurityEventMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsWildcardAllowedOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateValidator(new FakeAuthRepository(), new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "*",
            ["Security:RequireOriginForCookieAuth"] = "true"
        }));

        Assert.Equal("Security:AllowedOrigins must not contain wildcard origins.", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsEmptyAllowedOriginsOutsideDevelopment()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateValidator(new FakeAuthRepository(), new Dictionary<string, string?>
        {
            ["Security:RequireOriginForCookieAuth"] = "true"
        }));

        Assert.Equal("Security:AllowedOrigins must contain at least one origin outside Development.", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsDisabledOriginRequirementOutsideDevelopment()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateValidator(new FakeAuthRepository(), new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:RequireOriginForCookieAuth"] = "false"
        }));

        Assert.Equal("Security:RequireOriginForCookieAuth=false is only allowed in Development.", exception.Message);
    }

    private static CookieAuthOriginValidator CreateValidator(
        IAuthRepository repository,
        IReadOnlyDictionary<string, string?> values,
        string environmentName = "Production")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new CookieAuthOriginValidator(
            configuration,
            new FakeHostEnvironment(environmentName),
            repository,
            new FakeAuthClock(new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero)));
    }

    private static DefaultHttpContext CreateHttpContext(string? origin = null, string? referer = null)
    {
        var context = new DefaultHttpContext();
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        if (referer is not null)
        {
            context.Request.Headers.Referer = referer;
        }

        return context;
    }

    private static AuthRequestContext RequestContext()
    {
        return new AuthRequestContext("192.168.101.199", "unit-test", "trace-cookie-origin");
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "WeCms.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeAuthClock : IAuthClock
    {
        public FakeAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public int SecurityEventCount { get; private set; }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public string LastSecurityEventSeverity { get; private set; } = string.Empty;

        public string LastSecurityEventMessage { get; private set; } = string.Empty;

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            LastSecurityEventSeverity = record.Severity;
            LastSecurityEventMessage = record.Message;
            return Task.CompletedTask;
        }

        public Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
