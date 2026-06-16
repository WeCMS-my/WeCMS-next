namespace WeCms.Modules.System.System;

public interface ISystemDatabaseProbe
{
    Task<SystemDatabaseProbeResult> CheckAsync(CancellationToken cancellationToken);
}
