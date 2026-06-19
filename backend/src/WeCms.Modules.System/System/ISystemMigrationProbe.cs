namespace WeCms.Modules.System.System;

public interface ISystemMigrationProbe
{
    Task<SystemMigrationProbeResult> CheckAsync(CancellationToken cancellationToken);
}
