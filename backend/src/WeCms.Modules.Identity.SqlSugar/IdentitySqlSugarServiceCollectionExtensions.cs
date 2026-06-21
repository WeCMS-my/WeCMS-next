using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Identity.Repositories;
using WeCms.Modules.Identity.SqlSugar.Entities;
using WeCms.Modules.Identity.SqlSugar.Repositories;

namespace WeCms.Modules.Identity.SqlSugar;

public static class IdentitySqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsIdentitySqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, IdentityCodeFirstModelProvider>();
        services.AddScoped<AuthRepository>();
        services.AddScoped<IAuthRepository>(provider => provider.GetRequiredService<AuthRepository>());
        services.AddScoped<IAccountProfileRepository, AccountProfileRepository>();
        services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();
        services.AddScoped<ILoginFailureCounterRepository, LoginFailureCounterRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTwoFactorRepository, UserTwoFactorRepository>();

        return services;
    }

    private sealed class IdentityCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(AuthChallengeEntity),
                typeof(LoginFailureCounterEntity),
                typeof(RefreshTokenEntity),
                typeof(UserEntity),
                typeof(UserTwoFactorEntity)
            ];
        }
    }
}
