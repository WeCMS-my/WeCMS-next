using Dapper;
using WeCms.Infrastructure.Data;
using WeCms.Infrastructure.Migration;
using WeCms.Infrastructure.Security;
using WeCms.Infrastructure.Time;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITokenGenerator, CryptoTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddSingleton<DbMigrationRunner>();

        // Configure Dapper to use snake_case column mapping
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        return services;
    }

    public static IServiceCollection AddWeCmsAuth(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
