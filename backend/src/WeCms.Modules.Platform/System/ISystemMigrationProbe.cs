namespace WeCms.Modules.Platform.System;

public interface ISystemMigrationProbe
{
    Task<SystemMigrationProbeResult> CheckAsync(CancellationToken cancellationToken);
}
