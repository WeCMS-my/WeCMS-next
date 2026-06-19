using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WeCms.Api.Extensions;

public static class DatabaseStartupMigrationOptions
{
    public static bool ShouldRunMigrationsOnStartup(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configured = configuration["Database:RunMigrationsOnStartup"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return environment.IsDevelopment();
        }

        if (bool.TryParse(configured, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException("Database:RunMigrationsOnStartup must be true or false.");
    }
}
