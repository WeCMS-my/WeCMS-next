using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Posts;

public static class SystemPostsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemPosts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IPostService, PostService>();
        return services;
    }
}
