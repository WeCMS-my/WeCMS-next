using Dapper;
using Microsoft.Extensions.Configuration;
using WeCms.Shared.Data;
using WeCms.Shared.Security;

namespace WeCms.Persistence.Migration;

public sealed class DbMigrationRunner
{
    private const string SchemaTrackerTable = """
        CREATE TABLE IF NOT EXISTS `sys_schema_migration` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `version` VARCHAR(32) NOT NULL,
            `name` VARCHAR(256) NOT NULL,
            `checksum` VARCHAR(64) NOT NULL,
            `applied_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_version` (`version`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Schema迁移记录';
        """;

    private const string SeedTrackerTable = """
        CREATE TABLE IF NOT EXISTS `sys_seed_migration` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `version` VARCHAR(32) NOT NULL,
            `name` VARCHAR(256) NOT NULL,
            `checksum` VARCHAR(64) NOT NULL,
            `applied_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_version` (`version`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Seed迁移记录';
        """;

    private const string SelectSchemaChecksum = "SELECT checksum FROM `sys_schema_migration` WHERE version = @version LIMIT 1";
    private const string SelectSeedChecksum = "SELECT checksum FROM `sys_seed_migration` WHERE version = @version LIMIT 1";
    private const string InsertSchemaMigration = "INSERT INTO `sys_schema_migration` (version, name, checksum) VALUES (@version, @name, @checksum)";
    private const string InsertSeedMigration = "INSERT INTO `sys_seed_migration` (version, name, checksum) VALUES (@version, @name, @checksum)";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbMigrationScriptProvider _scriptProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public DbMigrationRunner(
        IDbConnectionFactory connectionFactory,
        IDbMigrationScriptProvider scriptProvider,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _scriptProvider = scriptProvider;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(SchemaTrackerTable, cancellationToken: cancellationToken));
        await ApplyScriptsAsync(
            connection,
            _scriptProvider.GetSchemaMigrations(),
            SelectSchemaChecksum,
            InsertSchemaMigration,
            "schema",
            cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(SeedTrackerTable, cancellationToken: cancellationToken));
        await ApplyScriptsAsync(
            connection,
            _scriptProvider.GetSeeds(),
            SelectSeedChecksum,
            InsertSeedMigration,
            "seed",
            cancellationToken);
    }

    private async Task ApplyScriptsAsync(
        global::System.Data.Common.DbConnection connection,
        IReadOnlyList<DbMigrationScript> scripts,
        string selectChecksumSql,
        string insertAppliedSql,
        string scriptKind,
        CancellationToken cancellationToken)
    {
        foreach (var script in scripts)
        {
            var appliedChecksum = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    selectChecksumSql,
                    new { version = script.Version },
                    cancellationToken: cancellationToken));

            if (appliedChecksum is not null)
            {
                if (!string.Equals(appliedChecksum, script.Checksum, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Checksum drift detected for {scriptKind} migration {script.Version} ({script.Name}). Applied checksum: {appliedChecksum}; current checksum: {script.Checksum}.");
                }

                continue;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(script.Sql, BuildParameters(script), cancellationToken: cancellationToken));
            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertAppliedSql,
                    new { version = script.Version, name = script.Name, checksum = script.Checksum },
                    cancellationToken: cancellationToken));
        }
    }

    private object? BuildParameters(DbMigrationScript script)
    {
        if (!script.Sql.Contains("@PasswordHash", StringComparison.Ordinal))
        {
            return null;
        }

        var seedAdminPassword = _configuration["Database:SeedAdminPassword"];
        if (string.IsNullOrWhiteSpace(seedAdminPassword))
        {
            throw new InvalidOperationException("Database:SeedAdminPassword 未配置，无法初始化超级管理员密码。");
        }

        return new { PasswordHash = _passwordHasher.Hash(seedAdminPassword) };
    }
}
