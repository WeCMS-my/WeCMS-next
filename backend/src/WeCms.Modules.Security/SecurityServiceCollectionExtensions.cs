using Microsoft.Extensions.DependencyInjection;
using WeCms.EventBus;
using WeCms.Modules.Security.Events;

namespace WeCms.Modules.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSecurity(this IServiceCollection services)
    {
        services.AddSingleton<ISecurityClock, SystemSecurityClock>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<ISecurityBanService, SecurityBanService>();
        services.AddScoped<IRateLimitSecurityEventService, RateLimitSecurityEventService>();
        services.AddSingleton(RateLimitHitBufferOptions.Default);
        services.AddSingleton(RateLimitSecurityEventFlushOptions.Default);
        services.AddSingleton<InMemoryRateLimitHitBuffer>();
        services.AddSingleton<IRateLimitHitBuffer>(provider => provider.GetRequiredService<InMemoryRateLimitHitBuffer>());
        services.AddSingleton<IRateLimitHitAggregator>(provider => provider.GetRequiredService<InMemoryRateLimitHitBuffer>());
        services.AddScoped<ISecurityAlertService, SecurityAlertService>();
        services.AddScoped<ISecurityAlertSink, LoggingSecurityAlertSink>();
        services.AddIntegrationEvent<SecurityBanCreatedEvent>(SecurityBanCreatedEvent.EventType);
        return services;
    }
}
