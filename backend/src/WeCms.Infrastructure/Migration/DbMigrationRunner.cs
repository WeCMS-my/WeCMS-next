using Dapper;
using WeCms.Infrastructure.Data;
using WeCms.Infrastructure.Security;

namespace WeCms.Infrastructure.Migration;

public sealed class DbMigrationRunner
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DbMigrationRunner(IDbConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenAsync(cancellationToken);

        // Ensure schema migration tracking table exists first
        await connection.ExecuteAsync(
            new CommandDefinition(MigrationSql.SchemaMigrationTable, cancellationToken: cancellationToken));

        // Apply migrations in order
        var migrations = new (string Version, string Name, string Sql, bool IsParameterized)[]
        {
            ("001", "init_m0_identity_tables", MigrationSql.IdentityTables, false),
            ("002", "init_m0_permission_tables", MigrationSql.PermissionTables, false),
            ("003", "init_m0_auth_security_tables", MigrationSql.AuthSecurityTables, false),
            ("004", "seed_m0_base_permissions", SeedSql.BasePermissions, false),
            ("005", "seed_m0_super_admin", SeedSql.SuperAdmin, true),
        };

        foreach (var (version, name, sql, isParameterized) in migrations)
        {
            var applied = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    "SELECT version FROM sys_schema_migration WHERE version = @version LIMIT 1",
                    new { version },
                    cancellationToken: cancellationToken));

            if (applied != null)
                continue;

            if (isParameterized)
            {
                // For seed with parameterized password hash
                var passwordHash = _passwordHasher.Hash("Admin@123");
                await connection.ExecuteAsync(
                    new CommandDefinition(sql, new { PasswordHash = passwordHash }, cancellationToken: cancellationToken));
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(sql, cancellationToken: cancellationToken));
            }

            var checksum = ComputeChecksum(sql);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO sys_schema_migration (version, name, checksum) VALUES (@version, @name, @checksum)",
                    new { version, name, checksum },
                    cancellationToken: cancellationToken));
        }
    }

    private static string ComputeChecksum(string sql)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(sql));
        return Convert.ToHexStringLower(bytes);
    }
}

internal static class MigrationSql
{
    public const string SchemaMigrationTable = """
        CREATE TABLE IF NOT EXISTS `sys_schema_migration` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `version` VARCHAR(32) NOT NULL,
            `name` VARCHAR(256) NOT NULL,
            `checksum` VARCHAR(64) NOT NULL DEFAULT '',
            `applied_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_version` (`version`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Schema迁移记录';
        """;

    public const string IdentityTables = """
        CREATE TABLE IF NOT EXISTS `sys_user` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `legacy_id` INT UNSIGNED NULL DEFAULT NULL,
            `username` VARCHAR(64) NOT NULL,
            `display_name` VARCHAR(128) NOT NULL DEFAULT '',
            `email` VARCHAR(256) NULL DEFAULT NULL,
            `phone` VARCHAR(32) NULL DEFAULT NULL,
            `avatar_file_id` BIGINT UNSIGNED NULL DEFAULT NULL,
            `password_hash` VARCHAR(512) NOT NULL DEFAULT '',
            `password_hash_algorithm` VARCHAR(32) NOT NULL DEFAULT 'pbkdf2-sha256',
            `password_migrated_at` DATETIME(3) NULL DEFAULT NULL,
            `status` TINYINT NOT NULL DEFAULT 1,
            `security_stamp` CHAR(36) NOT NULL DEFAULT '',
            `permission_version` INT UNSIGNED NOT NULL DEFAULT 0,
            `two_factor_enabled` TINYINT(1) NOT NULL DEFAULT 0,
            `two_factor_rebind_required` TINYINT(1) NOT NULL DEFAULT 0,
            `last_login_at` DATETIME(3) NULL DEFAULT NULL,
            `last_login_ip` VARCHAR(45) NULL DEFAULT NULL,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `updated_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
            `updated_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `deleted_at` DATETIME(3) NULL DEFAULT NULL,
            `deleted_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `row_version` INT UNSIGNED NOT NULL DEFAULT 1,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_username` (`username`),
            UNIQUE KEY `uk_email` (`email`),
            KEY `idx_deleted_at` (`deleted_at`),
            KEY `idx_status` (`status`),
            KEY `idx_legacy_id` (`legacy_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """;

    public const string PermissionTables = """
        CREATE TABLE IF NOT EXISTS `sys_role` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `legacy_id` INT UNSIGNED NULL DEFAULT NULL,
            `code` VARCHAR(64) NOT NULL,
            `name` VARCHAR(128) NOT NULL,
            `description` VARCHAR(512) NOT NULL DEFAULT '',
            `status` TINYINT NOT NULL DEFAULT 1,
            `is_system` TINYINT(1) NOT NULL DEFAULT 0,
            `is_builtin` TINYINT(1) NOT NULL DEFAULT 0,
            `sort_order` INT NOT NULL DEFAULT 0,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `updated_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
            `updated_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `deleted_at` DATETIME(3) NULL DEFAULT NULL,
            `deleted_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `row_version` INT UNSIGNED NOT NULL DEFAULT 1,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_code` (`code`),
            KEY `idx_deleted_at` (`deleted_at`),
            KEY `idx_legacy_id` (`legacy_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_user_role` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `user_id` BIGINT UNSIGNED NOT NULL,
            `role_id` BIGINT UNSIGNED NOT NULL,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_user_role` (`user_id`, `role_id`),
            KEY `idx_role_id` (`role_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_menu` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `legacy_id` INT UNSIGNED NULL DEFAULT NULL,
            `parent_id` BIGINT UNSIGNED NULL DEFAULT NULL,
            `code` VARCHAR(128) NOT NULL,
            `name` VARCHAR(128) NOT NULL,
            `icon` VARCHAR(64) NOT NULL DEFAULT '',
            `component` VARCHAR(256) NOT NULL DEFAULT '',
            `route_path` VARCHAR(256) NOT NULL DEFAULT '',
            `sort_order` INT NOT NULL DEFAULT 0,
            `status` TINYINT NOT NULL DEFAULT 1,
            `is_visible` TINYINT(1) NOT NULL DEFAULT 1,
            `is_system` TINYINT(1) NOT NULL DEFAULT 0,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `updated_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
            `updated_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `deleted_at` DATETIME(3) NULL DEFAULT NULL,
            `deleted_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `row_version` INT UNSIGNED NOT NULL DEFAULT 1,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_code` (`code`),
            KEY `idx_parent_id` (`parent_id`),
            KEY `idx_deleted_at` (`deleted_at`),
            KEY `idx_legacy_id` (`legacy_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_role_menu` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `role_id` BIGINT UNSIGNED NOT NULL,
            `menu_id` BIGINT UNSIGNED NOT NULL,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_role_menu` (`role_id`, `menu_id`),
            KEY `idx_menu_id` (`menu_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_permission` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `legacy_id` INT UNSIGNED NULL DEFAULT NULL,
            `code` VARCHAR(128) NOT NULL,
            `name` VARCHAR(128) NOT NULL,
            `module` VARCHAR(64) NOT NULL DEFAULT '',
            `resource` VARCHAR(64) NOT NULL DEFAULT '',
            `action` VARCHAR(64) NOT NULL DEFAULT '',
            `http_method` VARCHAR(16) NOT NULL DEFAULT '',
            `route_pattern` VARCHAR(256) NOT NULL DEFAULT '',
            `status` TINYINT NOT NULL DEFAULT 1,
            `is_system` TINYINT(1) NOT NULL DEFAULT 0,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `updated_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
            `updated_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `deleted_at` DATETIME(3) NULL DEFAULT NULL,
            `deleted_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            `row_version` INT UNSIGNED NOT NULL DEFAULT 1,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_code` (`code`),
            KEY `idx_module` (`module`),
            KEY `idx_deleted_at` (`deleted_at`),
            KEY `idx_legacy_id` (`legacy_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_role_permission` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `role_id` BIGINT UNSIGNED NOT NULL,
            `permission_id` BIGINT UNSIGNED NOT NULL,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `created_by` BIGINT UNSIGNED NULL DEFAULT NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_role_permission` (`role_id`, `permission_id`),
            KEY `idx_permission_id` (`permission_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """;

    public const string AuthSecurityTables = """
        CREATE TABLE IF NOT EXISTS `sys_refresh_token` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `user_id` BIGINT UNSIGNED NOT NULL,
            `token_hash` VARCHAR(512) NOT NULL,
            `family_id` CHAR(36) NOT NULL,
            `expires_at` DATETIME(3) NOT NULL,
            `revoked_at` DATETIME(3) NULL DEFAULT NULL,
            `replaced_by_token_id` BIGINT UNSIGNED NULL DEFAULT NULL,
            `created_ip` VARCHAR(45) NOT NULL DEFAULT '',
            `user_agent` VARCHAR(512) NOT NULL DEFAULT '',
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            UNIQUE KEY `uk_token_hash` (`token_hash`),
            KEY `idx_user_id` (`user_id`),
            KEY `idx_family_id` (`family_id`),
            KEY `idx_expires_at` (`expires_at`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_login_log` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `user_id` BIGINT UNSIGNED NULL DEFAULT NULL,
            `username` VARCHAR(64) NOT NULL DEFAULT '',
            `ip_address` VARCHAR(45) NOT NULL DEFAULT '',
            `user_agent` VARCHAR(512) NOT NULL DEFAULT '',
            `result` TINYINT NOT NULL,
            `fail_reason` VARCHAR(256) NOT NULL DEFAULT '',
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            KEY `idx_user_id` (`user_id`),
            KEY `idx_created_at` (`created_at`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

        CREATE TABLE IF NOT EXISTS `sys_security_event` (
            `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
            `user_id` BIGINT UNSIGNED NULL DEFAULT NULL,
            `event_type` VARCHAR(64) NOT NULL,
            `description` VARCHAR(512) NOT NULL DEFAULT '',
            `ip_address` VARCHAR(45) NOT NULL DEFAULT '',
            `user_agent` VARCHAR(512) NOT NULL DEFAULT '',
            `severity` TINYINT NOT NULL DEFAULT 0,
            `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            KEY `idx_user_id` (`user_id`),
            KEY `idx_event_type` (`event_type`),
            KEY `idx_created_at` (`created_at`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        """;
}

internal static class SeedSql
{
    public const string BasePermissions = """
        INSERT IGNORE INTO `sys_permission` (`code`, `name`, `module`, `resource`, `action`, `http_method`, `route_pattern`, `is_system`)
        VALUES
        ('sys:system:secure-ping', '安全探针', 'sys', 'system', 'secure-ping', 'GET', '/api/v1/system/secure-ping', 1);
        """;

    public const string SuperAdmin = """
        INSERT IGNORE INTO `sys_role` (`code`, `name`, `description`, `is_system`, `is_builtin`, `sort_order`)
        VALUES ('super_admin', '超级管理员', '系统内置超级管理员角色，拥有所有权限', 1, 1, 0);

        INSERT IGNORE INTO `sys_user` (`username`, `display_name`, `password_hash`, `password_hash_algorithm`, `status`, `security_stamp`)
        VALUES ('admin', '超级管理员', @PasswordHash, 'pbkdf2-sha256', 1, UUID());

        INSERT IGNORE INTO `sys_user_role` (`user_id`, `role_id`)
        SELECT u.id, r.id
        FROM `sys_user` u, `sys_role` r
        WHERE u.username = 'admin' AND r.code = 'super_admin';

        INSERT IGNORE INTO `sys_role_permission` (`role_id`, `permission_id`)
        SELECT r.id, p.id
        FROM `sys_role` r, `sys_permission` p
        WHERE r.code = 'super_admin' AND p.status = 1;
        """;
}
