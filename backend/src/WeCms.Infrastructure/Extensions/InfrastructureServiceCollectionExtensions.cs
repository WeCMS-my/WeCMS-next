using WeCms.Infrastructure.Id;
using WeCms.Infrastructure.Security;
using WeCms.Infrastructure.Time;
using WeCms.Shared.Id;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IIdGenerator, SystemGuidIdGenerator>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITokenGenerator, CryptoTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddSingleton<ICaptchaService, InMemoryCaptchaService>();
        services.AddSingleton<ITwoFactorLoginService, InMemoryTwoFactorLoginService>();

        return services;
    }
}
