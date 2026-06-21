using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.SqlSugar;
using WeCms.Modules.Audit.SqlSugar;
using WeCms.Modules.Configuration.SqlSugar;
using WeCms.Modules.FileCenter.SqlSugar;
using WeCms.Modules.Identity.SqlSugar;
using WeCms.Modules.Organization.SqlSugar;
using WeCms.Modules.Platform.SqlSugar;
using WeCms.Modules.Security.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class S10CodeFirstModelProviderTests
{
    private static readonly ModuleProviderExpectation[] ModuleProviderExpectations =
    [
        new("AccessControl", "WeCms.Modules.AccessControl.SqlSugar.Entities", services => services.AddWeCmsAccessControlSqlSugar(),
        [
            "sys_menu",
            "sys_permission",
            "sys_role",
            "sys_role_menu",
            "sys_role_permission",
            "sys_user_role"
        ]),
        new("Audit", "WeCms.Modules.Audit.SqlSugar.Entities", services => services.AddWeCmsAuditSqlSugar(),
        [
            "sys_audit_log",
            "sys_login_log"
        ]),
        new("Configuration", "WeCms.Modules.Configuration.SqlSugar.Entities", services => services.AddWeCmsConfigurationSqlSugar(),
        [
            "sys_dict_type",
            "sys_dict_value",
            "sys_i18n_message",
            "sys_setting"
        ]),
        new("FileCenter", "WeCms.Modules.FileCenter.SqlSugar.Entities", services => services.AddWeCmsFileCenterSqlSugar(),
        [
            "sys_file"
        ]),
        new("Identity", "WeCms.Modules.Identity.SqlSugar.Entities", services => services.AddWeCmsIdentitySqlSugar(),
        [
            "sys_auth_challenge",
            "sys_login_failure_counter",
            "sys_refresh_token",
            "sys_user",
            "sys_user_two_factor"
        ]),
        new("Organization", "WeCms.Modules.Organization.SqlSugar.Entities", services => services.AddWeCmsOrganizationSqlSugar(),
        [
            "sys_dept",
            "sys_position",
            "sys_user_position"
        ]),
        new("Platform", "WeCms.Modules.Platform.SqlSugar.Entities", services => services.AddWeCmsPlatformSqlSugar(),
        [
            "sys_schema_migration"
        ]),
        new("Security", "WeCms.Modules.Security.SqlSugar.Entities", services => services.AddWeCmsSecuritySqlSugar(),
        [
            "sys_security_ban",
            "sys_security_event"
        ])
    ];

    [Fact]
    public void ModuleSqlSugarProviders_ReturnBaselineEntityTables()
    {
        var services = new ServiceCollection();
        foreach (var expectation in ModuleProviderExpectations)
        {
            expectation.Register(services);
        }

        using var provider = services.BuildServiceProvider();
        var registry = new CodeFirstModelRegistry(provider.GetServices<ICodeFirstModelProvider>());

        var tableNames = registry.GetModelTypes()
            .Select(ResolveTableName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] expectedTableNames =
        [
            "sys_audit_log",
            "sys_auth_challenge",
            "sys_dept",
            "sys_dict_type",
            "sys_dict_value",
            "sys_file",
            "sys_i18n_message",
            "sys_login_failure_counter",
            "sys_login_log",
            "sys_menu",
            "sys_permission",
            "sys_refresh_token",
            "sys_role",
            "sys_role_menu",
            "sys_role_permission",
            "sys_schema_migration",
            "sys_security_ban",
            "sys_security_event",
            "sys_setting",
            "sys_user",
            "sys_user_position",
            "sys_user_role",
            "sys_user_two_factor",
            "sys_position"
        ];

        Assert.Equal(expectedTableNames.Order(StringComparer.Ordinal), tableNames);
    }

    [Fact]
    public void ModuleSqlSugarProviders_ReturnOnlyOwnedEntityTypes()
    {
        foreach (var expectation in ModuleProviderExpectations)
        {
            var services = new ServiceCollection();
            expectation.Register(services);

            using var provider = services.BuildServiceProvider();
            var modelTypes = provider.GetServices<ICodeFirstModelProvider>()
                .Single()
                .GetModelTypes()
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .ToArray();

            Assert.All(
                modelTypes,
                modelType => Assert.Equal(expectation.EntityNamespace, modelType.Namespace));
            Assert.Equal(
                expectation.TableNames.Order(StringComparer.Ordinal),
                modelTypes.Select(ResolveTableName).Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void ModuleSqlSugarProviders_DoNotReturnCmsEntities()
    {
        var services = new ServiceCollection();
        foreach (var expectation in ModuleProviderExpectations)
        {
            expectation.Register(services);
        }

        using var provider = services.BuildServiceProvider();
        var registry = new CodeFirstModelRegistry(provider.GetServices<ICodeFirstModelProvider>());

        Assert.DoesNotContain(
            registry.GetModelTypes(),
            modelType => modelType.FullName?.Contains(".Cms.", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string ResolveTableName(Type modelType)
    {
        foreach (var attribute in modelType.GetCustomAttributes(inherit: false))
        {
            var attributeType = attribute.GetType();
            if (attributeType.Name is not ("SugarTable" or "SugarTableAttribute"))
            {
                continue;
            }

            var tableName = attributeType.GetProperty("TableName")?.GetValue(attribute) as string;
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }
        }

        throw new InvalidOperationException($"CodeFirst model {modelType.FullName} must declare SugarTable.");
    }

    private sealed record ModuleProviderExpectation(
        string ModuleName,
        string EntityNamespace,
        Action<IServiceCollection> Register,
        string[] TableNames);
}
