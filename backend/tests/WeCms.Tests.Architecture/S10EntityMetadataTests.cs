namespace WeCms.Tests.Architecture;

public sealed class S10EntityMetadataTests
{
    private static readonly (string RelativePath, string[] Tokens)[] SharedEntityContracts =
    [
        ("WeCms.Shared/Data/IEntity.cs", ["public interface IEntity<TKey>", "TKey Id { get; set; }"]),
        ("WeCms.Shared/Data/ISoftDeleteEntity.cs", ["public interface ISoftDeleteEntity", "DateTime? DeletedAt { get; set; }"]),
        ("WeCms.Shared/Data/IAuditedEntity.cs", ["public interface IAuditedEntity", "DateTime CreatedAt { get; set; }", "DateTime UpdatedAt { get; set; }"]),
        ("WeCms.Shared/Data/ITenantEntity.cs", ["public interface ITenantEntity", "long TenantId { get; set; }"]),
        ("WeCms.Shared/Data/ISiteScopedEntity.cs", ["public interface ISiteScopedEntity", "long SiteId { get; set; }"]),
        ("WeCms.Shared/Data/IDataScopedEntity.cs", ["public interface IDataScopedEntity", "long CreatedByUserId { get; set; }"])
    ];

    private static readonly (string RelativePath, string[] Tokens)[] EntityBaseTypes =
    [
        ("WeCms.Data.SqlSugar/Entities/Common/EntityBase.cs", ["abstract class EntityBase", "IEntity<long>", "ISoftDeleteEntity", "IAuditedEntity", "[SugarColumn(IsPrimaryKey = true"]),
        ("WeCms.Data.SqlSugar/Entities/Common/TenantEntityBase.cs", ["abstract class TenantEntityBase", "EntityBase", "ITenantEntity", "public long TenantId { get; set; }"]),
        ("WeCms.Data.SqlSugar/Entities/Common/SiteScopedEntityBase.cs", ["abstract class SiteScopedEntityBase", "TenantEntityBase", "ISiteScopedEntity", "public long SiteId { get; set; }"])
    ];

    private static readonly (string Module, string FileName, string TableName, string[] RequiredTokens)[] BaselineEntities =
    [
        ("Identity", "UserEntity.cs", "sys_user", ["[SugarIndex(\"ux_sys_user_username\"", "[SugarColumn(Length = 64)", "public string Username", "public string PasswordHash"]),
        ("Identity", "RefreshTokenEntity.cs", "sys_refresh_token", ["[SugarIndex(\"ux_sys_refresh_token_hash\"", "public string TokenHash", "public string FamilyId"]),
        ("Identity", "LoginFailureCounterEntity.cs", "sys_login_failure_counter", ["[SugarIndex(\"ux_sys_login_failure_counter_scope_target\"", "public string Scope", "public string Target"]),
        ("Identity", "UserTwoFactorEntity.cs", "sys_user_two_factor", ["[SugarIndex(\"ux_sys_user_two_factor_user_id\"", "public long UserId", "public string SecretEncrypted"]),
        ("Identity", "AuthChallengeEntity.cs", "sys_auth_challenge", ["[SugarIndex(\"ux_sys_auth_challenge_challenge_id\"", "public string ChallengeId", "public string Status"]),
        ("AccessControl", "RoleEntity.cs", "sys_role", ["[SugarIndex(\"ux_sys_role_code\"", "public string Code", "public bool IsLocked"]),
        ("AccessControl", "UserRoleEntity.cs", "sys_user_role", ["public long UserId", "public long RoleId"]),
        ("AccessControl", "MenuEntity.cs", "sys_menu", ["[SugarIndex(\"ux_sys_menu_name\"", "public string Name", "public string Path"]),
        ("AccessControl", "PermissionEntity.cs", "sys_permission", ["[SugarIndex(\"ux_sys_permission_code\"", "public string Code", "public string Status", "public bool IsBuiltin"]),
        ("AccessControl", "RolePermissionEntity.cs", "sys_role_permission", ["public long RoleId", "public long PermissionId"]),
        ("AccessControl", "RoleMenuEntity.cs", "sys_role_menu", ["public long RoleId", "public long MenuId"]),
        ("Organization", "DepartmentEntity.cs", "sys_dept", ["[SugarIndex(\"ux_sys_dept_code\"", "public string Code", "public long? ParentId"]),
        ("Organization", "PositionEntity.cs", "sys_position", ["[SugarIndex(\"ux_sys_position_code\"", "public string Code", "public string Name"]),
        ("Organization", "UserPositionEntity.cs", "sys_user_position", ["public long UserId", "public long PositionId"]),
        ("Configuration", "DictTypeEntity.cs", "sys_dict_type", ["[SugarIndex(\"ux_sys_dict_type_code\"", "public string Code", "public string Status"]),
        ("Configuration", "DictValueEntity.cs", "sys_dict_value", ["[SugarIndex(\"ux_sys_dict_value_type_value\"", "public long TypeId", "public string Value"]),
        ("Configuration", "SettingEntity.cs", "sys_setting", ["[SugarIndex(\"ux_sys_setting_key\"", "public string Key", "public bool IsSensitive"]),
        ("Configuration", "I18nMessageEntity.cs", "sys_i18n_message", ["[SugarIndex(\"uq_sys_i18n_message_locale_key\"", "public string Locale", "public string MessageKey"]),
        ("Audit", "LoginLogEntity.cs", "sys_login_log", ["[SugarIndex(\"ix_sys_login_log_username\"", "public string Username", "public string Result"]),
        ("Audit", "AuditLogEntity.cs", "sys_audit_log", ["[SugarIndex(\"ix_sys_audit_log_created_at\"", "public string Module", "public string Action"]),
        ("Security", "SecurityEventEntity.cs", "sys_security_event", ["[SugarIndex(\"ix_sys_security_event_type\"", "public string EventType", "public string Severity"]),
        ("Security", "SecurityBanEntity.cs", "sys_security_ban", ["[SugarIndex(\"ix_sys_security_ban_lookup\"", "public string BanType", "public string Target"]),
        ("FileCenter", "FileEntity.cs", "sys_file", ["[SugarIndex(\"ux_sys_file_object_key\"", "public string ObjectKey", "public long SizeBytes"]),
        ("Platform", "SchemaMigrationEntity.cs", "sys_schema_migration", ["[SugarTable(\"sys_schema_migration\")", "public string Version", "public DateTime AppliedAt"])
    ];

    [Fact]
    public async Task SharedEntityContracts_ExistInSharedData()
    {
        await AssertFilesContainAsync(TestPaths.SourceRoot, SharedEntityContracts);
    }

    [Fact]
    public async Task EntityBaseTypes_ExistInDataSqlSugarBoundary()
    {
        await AssertFilesContainAsync(TestPaths.SourceRoot, EntityBaseTypes);
    }

    [Fact]
    public async Task BaselineSystemTables_HaveOwnedSqlSugarEntities()
    {
        foreach (var (module, fileName, tableName, requiredTokens) in BaselineEntities)
        {
            var path = Path.Combine(TestPaths.SourceRoot, $"WeCms.Modules.{module}.SqlSugar", "Entities", fileName);
            Assert.True(File.Exists(path), $"Missing entity for {tableName}: {Path.GetRelativePath(TestPaths.RepoRoot, path)}");

            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains($"[SugarTable(\"{tableName}\")]", source, StringComparison.Ordinal);
            Assert.True(
                source.Contains("[SugarColumn(IsPrimaryKey = true", StringComparison.Ordinal)
                    || source.Contains(": EntityBase", StringComparison.Ordinal)
                    || source.Contains(": TenantEntityBase", StringComparison.Ordinal)
                    || source.Contains(": SiteScopedEntityBase", StringComparison.Ordinal),
                $"{Path.GetRelativePath(TestPaths.RepoRoot, path)} must define or inherit a primary key.");
            foreach (var token in requiredTokens)
            {
                Assert.Contains(token, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task BusinessModules_DoNotDependOnEntityBaseInfrastructure()
    {
        foreach (var path in Directory.EnumerateFiles(TestPaths.SourceRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var projectName = Path.GetFileNameWithoutExtension(path);
            if (!projectName.StartsWith("WeCms.Modules.", StringComparison.Ordinal) || projectName.EndsWith(".SqlSugar", StringComparison.Ordinal))
            {
                continue;
            }

            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.DoesNotContain("WeCms.Data.SqlSugar", source, StringComparison.Ordinal);
        }
    }

    private static async Task AssertFilesContainAsync(string sourceRoot, IEnumerable<(string RelativePath, string[] Tokens)> expectations)
    {
        foreach (var (relativePath, tokens) in expectations)
        {
            var path = Path.Combine(sourceRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing expected file: backend/src/{relativePath}");

            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            foreach (var token in tokens)
            {
                Assert.Contains(token, source, StringComparison.Ordinal);
            }
        }
    }
}
