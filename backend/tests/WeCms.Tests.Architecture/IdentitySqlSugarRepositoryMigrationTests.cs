namespace WeCms.Tests.Architecture;

public sealed class IdentitySqlSugarRepositoryMigrationTests
{
    private static readonly string IdentitySqlSugarRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar");

    private static readonly string PersistenceRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.Persistence);

    private static readonly string[] IdentityRepositoryImplementationFiles =
    [
        Path.Combine("Repositories", "AccountProfileRepository.cs"),
        Path.Combine("Repositories", "AuthChallengeRepository.cs"),
        Path.Combine("Repositories", "AuthRepository.cs"),
        Path.Combine("Repositories", "LoginFailureCounterRepository.cs"),
        Path.Combine("Repositories", "UserTwoFactorRepository.cs"),
        Path.Combine("Repositories", "UserRepository.cs"),
        Path.Combine("Repositories", "UserRepository.Helpers.cs")
    ];

    private static readonly string[] LegacyPersistenceIdentityRepositoryFiles =
    [
        Path.Combine("Modules", "System", "Auth", "AccountProfileRepository.cs"),
        Path.Combine("Modules", "System", "Auth", "AuthChallengeRepository.cs"),
        Path.Combine("Modules", "System", "Auth", "AuthRepository.cs"),
        Path.Combine("Modules", "System", "Auth", "LoginFailureCounterRepository.cs"),
        Path.Combine("Modules", "System", "TwoFactor", "UserTwoFactorRepository.cs"),
        Path.Combine("Modules", "System", "Users", "UserRepository.cs"),
        Path.Combine("Modules", "System", "Users", "UserRepository.Helpers.cs")
    ];

    [Fact]
    public void IdentityRepositoryImplementations_LiveInIdentitySqlSugar()
    {
        foreach (var relativePath in IdentityRepositoryImplementationFiles)
        {
            var path = Path.Combine(IdentitySqlSugarRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing Identity SqlSugar repository implementation: {relativePath}");
        }
    }

    [Fact]
    public async Task IdentityRepositoryImplementations_UseIdentitySqlSugarNamespace()
    {
        foreach (var relativePath in IdentityRepositoryImplementationFiles)
        {
            var source = await File.ReadAllTextAsync(Path.Combine(IdentitySqlSugarRoot, relativePath), TestContext.Current.CancellationToken);
            Assert.Contains("namespace WeCms.Modules.Identity.SqlSugar.Repositories;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.Persistence + ".Modules." + "System.", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacyPersistenceIdentityRepositoryImplementations_AreRemoved()
    {
        var remaining = LegacyPersistenceIdentityRepositoryFiles
            .Select(relativePath => Path.Combine(PersistenceRoot, relativePath))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(PersistenceRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy Persistence identity repository implementations remain: " + string.Join(", ", remaining));
    }

    [Fact]
    public async Task DataSqlSugarRegistration_DoesNotOwnIdentityRepositories()
    {
        var files = Directory.EnumerateFiles(
                Path.Combine(TestPaths.SourceRoot, "WeCms.Data.SqlSugar"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);
        var chunks = new List<string>();
        foreach (var file in files)
        {
            chunks.Add(await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));
        }

        var source = string.Join('\n', chunks);

        Assert.DoesNotContain(LegacyBoundaryNames.PersistenceSystemNamespace("Auth"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(LegacyBoundaryNames.PersistenceSystemNamespace("TwoFactor"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(LegacyBoundaryNames.PersistenceSystemNamespace("Users"), source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuthRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccountProfileRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuthChallengeRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ILoginFailureCounterRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUserRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUserTwoFactorRepository", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentitySqlSugarRegistration_OwnsIdentityRepositories()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(IdentitySqlSugarRoot, "IdentitySqlSugarServiceCollectionExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("services.AddScoped<AuthRepository>();", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IAuthRepository>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdentityAccessProfileReader", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IAccountProfileRepository, AccountProfileRepository>();", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<ILoginFailureCounterRepository, LoginFailureCounterRepository>();", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IUserRepository, UserRepository>();", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<IUserTwoFactorRepository, UserTwoFactorRepository>();", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentitySqlSugarProject_DoesNotReferencePersistenceOrSystem()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(IdentitySqlSugarRoot, "WeCms.Modules.Identity.SqlSugar.csproj"),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(LegacyBoundaryNames.Persistence, source, StringComparison.Ordinal);
        Assert.DoesNotContain(LegacyBoundaryNames.SystemModule, source, StringComparison.Ordinal);
        Assert.DoesNotContain("WeCms.Modules.AccessControl", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestsAndScripts_DoNotReferenceLegacyPersistenceIdentityPaths()
    {
        var roots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "backend", "tests"),
            Path.Combine(TestPaths.RepoRoot, "scripts")
        };
        var files = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".sh", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(nameof(IdentitySqlSugarRepositoryMigrationTests) + ".cs", StringComparison.Ordinal));
        var forbiddenTokens = new[]
        {
            LegacyBoundaryNames.PersistenceSystemNamespace("Auth"),
            LegacyBoundaryNames.PersistenceSystemNamespace("TwoFactor"),
            LegacyBoundaryNames.PersistenceSystemNamespace("Users"),
            LegacyBoundaryNames.PersistenceSystemSourcePath("Auth"),
            LegacyBoundaryNames.PersistenceSystemSourcePath("TwoFactor"),
            LegacyBoundaryNames.PersistenceSystemSourcePath("Users")
        };

        var violations = new List<string>();
        foreach (var file in files)
        {
            var source = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            foreach (var token in forbiddenTokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
