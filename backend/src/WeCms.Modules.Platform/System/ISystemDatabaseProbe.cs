namespace WeCms.Modules.Platform.System;

public interface ISystemDatabaseProbe
{
    Task<SystemDatabaseProbeResult> CheckAsync(CancellationToken cancellationToken);
}
