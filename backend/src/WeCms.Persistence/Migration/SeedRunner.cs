using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SqlSugar;

namespace WeCms.Persistence.Migration;

public sealed class SeedRunner : ISeedRunner
{
    private const string DefaultDevelopmentAdminPassword = "Admin@123";
    private const int PasswordIterations = 600_000;
    private const int MinimumProductionPasswordLength = 12;
    private readonly ISqlSugarClient _db;

    public SeedRunner(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<string>> SeedAsync(
        string seedsDirectory,
        SeedRunnerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedsDirectory);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(seedsDirectory))
        {
            throw new DirectoryNotFoundException($"Seed directory does not exist: {seedsDirectory}");
        }

        var adminPassword = ResolveAdminPassword(options);
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{ADMIN_PASSWORD_HASH}}"] = SqlLiteral(HashPassword(adminPassword))
        };
        var executed = new List<string>();

        foreach (var file in Directory.EnumerateFiles(seedsDirectory, "*.sql").Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sql = File.ReadAllText(file);
            foreach (var replacement in replacements)
            {
                sql = sql.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
            }

            EnsureNoUnresolvedPlaceholders(file, sql);

            _db.Ado.BeginTran();
            try
            {
                foreach (var statement in DbMigrationRunner.SplitSqlStatements(sql))
                {
                    _db.Ado.ExecuteCommand(statement);
                }

                _db.Ado.CommitTran();
                executed.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(executed);
    }

    private static string ResolveAdminPassword(SeedRunnerOptions options)
    {
        if (string.Equals(options.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(options.AdminPassword)
                ? DefaultDevelopmentAdminPassword
                : options.AdminPassword;
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword)
            || string.Equals(options.AdminPassword, DefaultDevelopmentAdminPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Database:SeedAdminPassword must be configured outside Development and cannot use the development default.");
        }

        if (!IsStrongProductionPassword(options.AdminPassword))
        {
            throw new InvalidOperationException(
                $"Database:SeedAdminPassword must be at least {MinimumProductionPasswordLength} characters and include uppercase, lowercase, digit, and symbol characters.");
        }

        return options.AdminPassword;
    }

    private static bool IsStrongProductionPassword(string password)
    {
        return password.Length >= MinimumProductionPasswordLength
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    private static void EnsureNoUnresolvedPlaceholders(string file, string sql)
    {
        var match = Regex.Match(sql, @"\{\{[A-Z0-9_]+\}\}", RegexOptions.CultureInvariant);
        if (match.Success)
        {
            throw new InvalidOperationException(
                $"Seed file {Path.GetFileName(file)} contains unresolved placeholder {match.Value}.");
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            32);

        return $"wecms.pbkdf2-sha256.v1.{PasswordIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static string SqlLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
