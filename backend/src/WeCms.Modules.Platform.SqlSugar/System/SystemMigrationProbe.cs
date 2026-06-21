using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;
using WeCms.Modules.Platform.System;

namespace WeCms.Modules.Platform.SqlSugar.System;

public sealed class SystemMigrationProbe : ISystemMigrationProbe
{
    private const string FailureCode = "migration_status_unavailable";
    private const string MissingLatestMigrationFailureCode = "latest_required_migration_missing";
    private const string MissingConfigurationFailureCode = "latest_required_migration_not_configured";
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemMigrationProbe> _logger;

    public SystemMigrationProbe(ISqlSugarClient db, IConfiguration configuration, ILogger<SystemMigrationProbe> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<SystemMigrationProbeResult> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var latestRequiredMigration = _configuration["Database:LatestRequiredMigration"];
            if (string.IsNullOrWhiteSpace(latestRequiredMigration))
            {
                return Task.FromResult(SystemMigrationProbeResult.Unavailable(MissingConfigurationFailureCode));
            }

            var started = Stopwatch.GetTimestamp();
            var value = _db.Ado.GetScalar(
                "SELECT COUNT(*) FROM sys_schema_migration WHERE version = @version",
                new SugarParameter("@version", latestRequiredMigration.Trim()));
            var matchingCount = global::System.Convert.ToInt64(value, global::System.Globalization.CultureInfo.InvariantCulture);
            var latencyMs = Convert.ToInt64(Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds));

            return Task.FromResult(matchingCount == 1
                ? SystemMigrationProbeResult.Ok(matchingCount, latencyMs)
                : SystemMigrationProbeResult.Unavailable(MissingLatestMigrationFailureCode));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _logger.LogWarning(
                "System migration probe failed. FailureCode: {FailureCode}",
                FailureCode);

            return Task.FromResult(SystemMigrationProbeResult.Unavailable(FailureCode));
        }
    }
}
