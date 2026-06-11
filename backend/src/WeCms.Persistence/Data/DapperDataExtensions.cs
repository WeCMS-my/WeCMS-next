using Dapper;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.System.Auth;
using WeCms.Persistence.Migration;
using WeCms.Persistence.System.Auth;
using WeCms.Persistence.System.Permissions;
using WeCms.Shared.Data;
using WeCms.Shared.Security;

namespace WeCms.Persistence.Data;

public static class DapperDataExtensions
{
    public static IServiceCollection AddWeCmsPersistence(this IServiceCollection services)
    {
        // Data access infrastructure
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Migration runner
        services.AddSingleton<DbMigrationRunner>();

        // Repository implementations
        services.AddScoped<IAuthRepository, AuthRepository>();

        // Permission checker
        services.AddSingleton<IPermissionChecker, PermissionChecker>();

        // Configure Dapper to use snake_case column mapping
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        return services;
    }
}
