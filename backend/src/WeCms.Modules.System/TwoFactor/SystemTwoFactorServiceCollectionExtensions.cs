using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.TwoFactor;

public static class SystemTwoFactorServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemTwoFactor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new TwoFactorOptions(
            Required(configuration, "Security:TwoFactor:SecretProtectionKey"),
            configuration["Security:TwoFactor:Issuer"] ?? "WeCMS",
            ReadInt(configuration, "Security:TwoFactor:PeriodSeconds", 30),
            ReadInt(configuration, "Security:TwoFactor:CodeDigits", 6),
            ReadInt(configuration, "Security:TwoFactor:AllowedWindowSteps", 1),
            ReadInt(configuration, "Security:TwoFactor:RecoveryCodeCount", 10));

        services.AddSingleton(options);
        services.AddSingleton<TwoFactorEntropy>();
        services.AddSingleton<ITotpSecretEntropy>(sp => sp.GetRequiredService<TwoFactorEntropy>());
        services.AddSingleton<IRecoveryCodeEntropy>(sp => sp.GetRequiredService<TwoFactorEntropy>());
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<IRecoveryCodeService, RecoveryCodeService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        return services;
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} must be configured.");
        }

        return value;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, global::System.Globalization.NumberStyles.None, global::System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"{key} must be an integer.");
        }

        return result;
    }
}
