using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Users;

public static class SystemUsersServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemUsers(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
