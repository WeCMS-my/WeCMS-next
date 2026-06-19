using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.I18n;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Security;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.System;
using WeCms.Modules.System.TwoFactor;
using WeCms.Modules.System.Users;
using WeCms.Persistence.Modules.System.Auth;
using WeCms.Persistence.Modules.System.Departments;
using WeCms.Persistence.Modules.System.Dicts;
using WeCms.Persistence.Modules.System.Files;
using WeCms.Persistence.Modules.System.I18n;
using WeCms.Persistence.Modules.System.Menus;
using WeCms.Persistence.Modules.System.Logs;
using WeCms.Persistence.Modules.System.Permissions;
using WeCms.Persistence.Modules.System.Posts;
using WeCms.Persistence.Modules.System.Roles;
using WeCms.Persistence.Modules.System.Security;
using WeCms.Persistence.Modules.System.Settings;
using WeCms.Persistence.Modules.System.System;
using WeCms.Persistence.Modules.System.TwoFactor;
using WeCms.Persistence.Modules.System.Users;
using WeCms.Persistence.Migration;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useMigrationConnectionString = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = useMigrationConnectionString
            ? configuration.GetConnectionString("Migration") ?? configuration.GetConnectionString("Default")
            : configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new PersistenceConfigurationException("ConnectionStrings:Default is required for WeCMS persistence.");
        }

        var databaseOptions = DatabaseOptions.FromConfiguration(configuration);

        services.AddSingleton(databaseOptions);
        services.AddScoped<ISqlSugarClientFactory>(sp => new SqlSugarClientFactory(
            connectionString,
            sp.GetRequiredService<DatabaseOptions>()));
        services.AddScoped<ISqlSugarClient>(sp => sp.GetRequiredService<ISqlSugarClientFactory>().Create());
        services.AddScoped<IUnitOfWork, SqlSugarUnitOfWork>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAccountProfileRepository, AccountProfileRepository>();
        services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();
        services.AddScoped<ILoginFailureCounterRepository, LoginFailureCounterRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDictRepository, DictRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<II18nMessageRepository, I18nMessageRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionSecurityEventWriter, PermissionSecurityEventRepository>();
        services.AddScoped<IPermissionVersionRepository, PermissionVersionRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISecurityBanRepository, SecurityBanRepository>();
        services.AddScoped<IRateLimitSecurityEventRepository, RateLimitSecurityEventRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTwoFactorRepository, UserTwoFactorRepository>();
        services.AddScoped<ISystemDatabaseProbe, SystemDatabaseProbe>();
        services.AddScoped<IDbMigrationRunner, DbMigrationRunner>();
        services.AddScoped<ISeedRunner, SeedRunner>();

        return services;
    }
}
