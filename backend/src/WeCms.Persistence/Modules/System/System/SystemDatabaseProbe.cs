using Microsoft.Extensions.Logging;
using SqlSugar;
using WeCms.Modules.System.System;

namespace WeCms.Persistence.Modules.System.System;

public sealed class SystemDatabaseProbe : ISystemDatabaseProbe
{
    private const string FailureCode = "database_unavailable";
    private readonly ISqlSugarClient _db;
    private readonly ILogger<SystemDatabaseProbe> _logger;

    public SystemDatabaseProbe(ISqlSugarClient db, ILogger<SystemDatabaseProbe> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<SystemDatabaseProbeResult> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var value = _db.Ado.GetScalar("SELECT 1");
            var available = global::System.Convert.ToInt32(value, global::System.Globalization.CultureInfo.InvariantCulture) == 1;

            return Task.FromResult(available
                ? SystemDatabaseProbeResult.Ok
                : SystemDatabaseProbeResult.Unavailable(FailureCode));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _logger.LogWarning(
                "System database probe failed. FailureCode: {FailureCode}",
                FailureCode);

            return Task.FromResult(SystemDatabaseProbeResult.Unavailable(FailureCode));
        }
    }
}
