namespace WeCms.Tests.Architecture;

public sealed class IdentityDiMigrationTests
{
    private static readonly string ApiProgramPath = Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Program.cs");

    private static readonly string IdentityRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    [Fact]
    public async Task Program_UsesIdentityDiInsteadOfLegacySystemIdentityDi()
    {
        var source = await File.ReadAllTextAsync(ApiProgramPath, TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsIdentity(builder.Configuration);", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddWeCmsIdentitySqlSugar();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddWeCmsSystemAuth", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddWeCmsSystemTwoFactor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddWeCmsSystemUsers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Auth") + ";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("TwoFactor") + ";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using " + LegacyBoundaryNames.SystemNamespace("Users") + ";", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityDiExtension_RegistersIdentityServicesAndAuthentication()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(IdentityRoot, "IdentityServiceCollectionExtensions.cs"),
            TestContext.Current.CancellationToken);

        var requiredTokens = new[]
        {
            "IServiceCollection AddWeCmsIdentity(this IServiceCollection services, IConfiguration configuration)",
            "services.AddScoped<IAuthService, AuthService>();",
            "services.AddScoped<IAuthAuditWriter, AuthAuditWriter>();",
            "services.AddScoped<IAuthSecurityEventWriter, AuthSecurityEventWriter>();",
            "services.AddScoped<IRefreshTokenRotationService, RefreshTokenRotationService>();",
            "services.AddScoped<ILogoutTokenRevoker, LogoutTokenRevoker>();",
            "services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();",
            "services.AddScoped<IAuthTwoFactorChallengeService, AuthTwoFactorChallengeService>();",
            "services.AddScoped<IAccountTwoFactorService, AccountTwoFactorService>();",
            "services.AddScoped<IAccountProfileService, AccountProfileService>();",
            "services.AddScoped<IUserService, UserService>();",
            "services.AddScoped<ITwoFactorService, TwoFactorService>();",
            "services.AddScoped<IIdentityCookieAuthOriginValidator>(provider => provider.GetRequiredService<CookieAuthOriginValidator>());",
            "services.AddAuthentication(AccessTokenAuthenticationHandler.SchemeName)",
            ".AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>("
        };

        foreach (var token in requiredTokens)
        {
            Assert.Contains(token, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemIdentityDiExtensions_AreRemoved()
    {
        var legacyFiles = new[]
        {
            Path.Combine(SystemRoot, "Auth", "SystemAuthServiceCollectionExtensions.cs"),
            Path.Combine(SystemRoot, "TwoFactor", "SystemTwoFactorServiceCollectionExtensions.cs"),
            Path.Combine(SystemRoot, "Users", "SystemUsersServiceCollectionExtensions.cs")
        };

        var remaining = legacyFiles.Where(File.Exists).ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy System identity DI extensions remain: " + string.Join(", ", remaining.Select(path => Path.GetRelativePath(SystemRoot, path))));
    }

    [Fact]
    public async Task AuthRuntimeSupport_LivesInIdentityModule()
    {
        var identityFiles = new[]
        {
            Path.Combine(IdentityRoot, "Services", "AccessTokenAuthenticationHandler.cs"),
            Path.Combine(IdentityRoot, "Services", "CookieAuthOriginValidation.cs")
        };

        foreach (var path in identityFiles)
        {
            Assert.True(File.Exists(path), $"Missing Identity auth runtime file: {path}");
            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("namespace WeCms.Modules.Identity.Services;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemNamespace("Auth") + ";", source, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(Path.Combine(SystemRoot, "Auth", "AccessTokenAuthenticationHandler.cs")));
        Assert.False(File.Exists(Path.Combine(SystemRoot, "Auth", "CookieAuthOriginValidation.cs")));
    }
}
