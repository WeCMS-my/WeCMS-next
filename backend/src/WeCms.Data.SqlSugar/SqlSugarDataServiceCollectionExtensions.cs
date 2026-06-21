using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public static class SqlSugarDataServiceCollectionExtensions
{
    private const string DefaultConnectionName = "main";

    public static IServiceCollection AddWeCmsSqlSugarData(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useMigrationConnectionString = false,
        string codeFirstEnvironmentName = "Unknown")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var databaseOptions = CreateDatabaseOptions(configuration, useMigrationConnectionString);

        services.AddSingleton(databaseOptions);
        services.AddSingleton<SqlSugarConnectionRegistry>();
        services.AddSingleton(new SqlAuditOptions());
        services.AddSingleton<ISqlAuditSink, NullSqlAuditSink>();
        services.AddSingleton<ISqlAuditContextAccessor, AmbientSqlAuditContextAccessor>();
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        services.AddScoped<SqlAuditRecorder>();
        services.AddScoped<SqlSugarSqlAuditRegistrar>();
        services.AddScoped<ISqlSugarClientFactory>(sp => new SqlSugarClientFactory(
            sp.GetRequiredService<DatabasePlatformOptions>(),
            sp.GetServices<ISqlSugarQueryFilterRegistrar>(),
            sp.GetServices<ISqlSugarAuditRegistrar>(),
            sp.GetRequiredService<ITenantConnectionResolver>()));
        services.AddScoped<ISqlSugarClient>(sp => sp.GetRequiredService<ISqlSugarClientFactory>().Create());
        services.AddScoped<ICodeFirstModelRegistry>(sp => new CodeFirstModelRegistry(sp.GetServices<ICodeFirstModelProvider>()));
        services.AddScoped<IQueryFilterBypassAuditSink, NullQueryFilterBypassAuditSink>();
        services.AddScoped<QueryFilterBypass>();
        services.AddScoped<IQueryFilterContextAccessor, AmbientQueryFilterContextAccessor>();
        services.AddScoped<QueryFilterRegistrar>();
        services.AddScoped<IQueryFilterRegistrar>(sp => sp.GetRequiredService<QueryFilterRegistrar>());
        services.AddScoped<ISqlSugarQueryFilterRegistrar>(sp => sp.GetRequiredService<QueryFilterRegistrar>());
        services.AddScoped<ISqlSugarAuditRegistrar>(sp => sp.GetRequiredService<SqlSugarSqlAuditRegistrar>());
        services.AddScoped<ICodeFirstRunner>(sp => new SqlSugarCodeFirstRunner(
            sp.GetRequiredService<ICodeFirstModelRegistry>(),
            sp.GetRequiredService<ISqlSugarClient>(),
            codeFirstEnvironmentName));
        services.AddScoped<ISqlSugarSchemaValidator>(sp => new SqlSugarSchemaValidator(
            sp.GetRequiredService<ICodeFirstModelRegistry>(),
            sp.GetRequiredService<ISqlSugarClient>()));
        services.AddScoped<IUnitOfWork, SqlSugarUnitOfWork>();
        services.AddScoped<IDbMigrationRunner, DbMigrationRunner>();
        services.AddScoped<ISeedRunner, SeedRunner>();

        return services;
    }

    private static DatabasePlatformOptions CreateDatabaseOptions(
        IConfiguration configuration,
        bool useMigrationConnectionString)
    {
        if (configuration.GetSection("Database:Connections").GetChildren().Any())
        {
            return new DatabaseOptionsReader().Read(configuration);
        }

        var connectionStringName = useMigrationConnectionString && !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Migration"))
            ? "Migration"
            : "Default";
        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DatabaseConfigurationException($"ConnectionStrings:{connectionStringName} is required for WeCMS data access.");
        }

        return CreateLegacyDatabaseOptions(
            connectionStringName,
            connectionString,
            ReadCommandTimeout(configuration));
    }

    private static DatabasePlatformOptions CreateLegacyDatabaseOptions(
        string connectionStringName,
        string connectionString,
        int commandTimeoutSeconds)
    {
        return new DatabasePlatformOptions(
            DefaultConnectionName,
            [
                new DatabaseConnectionOptions(
                    DefaultConnectionName,
                    DbType.MySql,
                    connectionStringName,
                    connectionString,
                    DatabaseConnectionRole.Main,
                    true,
                    commandTimeoutSeconds)
            ]);
    }

    private static int ReadCommandTimeout(IConfiguration configuration)
    {
        var value = configuration["Database:CommandTimeoutSeconds"];
        if (string.IsNullOrWhiteSpace(value))
        {
            return DatabasePlatformOptions.DefaultCommandTimeoutSeconds;
        }

        if (!int.TryParse(value, out var parsed)
            || parsed < DatabasePlatformOptions.MinimumCommandTimeoutSeconds
            || parsed > DatabasePlatformOptions.MaximumCommandTimeoutSeconds)
        {
            throw new DatabaseConfigurationException(
                $"Database:CommandTimeoutSeconds must be an integer between {DatabasePlatformOptions.MinimumCommandTimeoutSeconds} and {DatabasePlatformOptions.MaximumCommandTimeoutSeconds}.");
        }

        return parsed;
    }
}
