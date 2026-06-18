using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.I18n;

public static class SystemI18nServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemI18n(this IServiceCollection services)
    {
        services.AddScoped<II18nMessageService, I18nMessageService>();
        return services;
    }
}

