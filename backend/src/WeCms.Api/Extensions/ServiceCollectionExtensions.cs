using WeCms.Shared.Contracts;
using WeCms.Infrastructure.Data;
using WeCms.Infrastructure.Security;
using WeCms.Modules.System.Auth;

namespace WeCms.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("CS missing");
        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(cs));
        var secret = configuration["Auth:JwtSecret"] ?? "wecms-dev-secret-change-in-production-32chars";
        services.AddSingleton<ITokenService>(new TokenService(secret, 900));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
        return services;
    }
}