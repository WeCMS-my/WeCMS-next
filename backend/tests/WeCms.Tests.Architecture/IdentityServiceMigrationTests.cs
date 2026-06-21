namespace WeCms.Tests.Architecture;

public sealed class IdentityServiceMigrationTests
{
    private static readonly string IdentityRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    private static readonly string[] IdentityServiceFiles =
    [
        Path.Combine("Services", "AuthService.cs"),
        Path.Combine("Services", "AuthSessionIssuer.cs"),
        Path.Combine("Services", "RefreshTokenRotationService.cs"),
        Path.Combine("Services", "LogoutTokenRevoker.cs"),
        Path.Combine("Services", "AuthTwoFactorChallengeService.cs"),
        Path.Combine("Services", "AccountTwoFactorService.cs"),
        Path.Combine("Services", "AccountProfileService.cs"),
        Path.Combine("Services", "UserService.cs"),
        Path.Combine("Services", "TwoFactorService.cs"),
        Path.Combine("Services", "AuthSecurity.cs"),
        Path.Combine("Services", "TwoFactorSecurity.cs"),
        Path.Combine("Services", "LoginFailureLimiter.cs")
    ];

    private static readonly string[] LegacyIdentityServiceFiles =
    [
        Path.Combine("Auth", "AuthService.cs"),
        Path.Combine("Auth", "AuthSessionIssuer.cs"),
        Path.Combine("Auth", "RefreshTokenRotationService.cs"),
        Path.Combine("Auth", "LogoutTokenRevoker.cs"),
        Path.Combine("Auth", "AuthTwoFactorChallengeService.cs"),
        Path.Combine("Auth", "AccountTwoFactorService.cs"),
        Path.Combine("Auth", "AccountProfileService.cs"),
        Path.Combine("Users", "UserService.cs"),
        Path.Combine("TwoFactor", "TwoFactorService.cs"),
        Path.Combine("Auth", "AuthSecurity.cs"),
        Path.Combine("TwoFactor", "TwoFactorSecurity.cs"),
        Path.Combine("Auth", "LoginFailureLimiter.cs")
    ];

    [Fact]
    public void IdentityServiceFiles_LiveInIdentityModule()
    {
        foreach (var relativePath in IdentityServiceFiles)
        {
            var path = Path.Combine(IdentityRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing Identity service file: {relativePath}");
        }
    }

    [Fact]
    public async Task IdentityServiceFiles_DoNotDependOnLegacySystemNamespace()
    {
        foreach (var relativePath in IdentityServiceFiles)
        {
            var source = await File.ReadAllTextAsync(Path.Combine(IdentityRoot, relativePath), TestContext.Current.CancellationToken);
            Assert.Contains("namespace WeCms.Modules.Identity.Services;", source, StringComparison.Ordinal);
            Assert.DoesNotContain(LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemIdentityServiceFiles_AreRemoved()
    {
        var remaining = LegacyIdentityServiceFiles
            .Select(relativePath => Path.Combine(SystemRoot, relativePath))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(SystemRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy System identity service files remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task AuthRepositoryInterface_DoesNotExposeAccessControlProfileQueries()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(IdentityRoot, "Repositories", "IAuthRepository.cs"),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("ListRoleCodesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListPermissionCodesAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ListVisibleMenusAsync", source, StringComparison.Ordinal);
    }
}
