namespace WeCms.Modules.Platform.System;

public sealed record SystemLiveResponse(string Status);

public sealed record SystemReadyResponse(string Status, bool Database, bool Migrations, bool CriticalConfiguration);

public sealed record SystemDependencyStatus(string Status, bool Available, long? LatencyMs, string? FailureCode);

public sealed record SystemDependenciesResponse(
    string Status,
    SystemDependencyStatus Database,
    SystemDependencyStatus Migrations,
    SystemDependencyStatus CriticalConfiguration);

public sealed record SystemPingResponse(string Status);

public sealed record SecurePingResponse(string Status);

public sealed record SystemVersionResponse(string Version);

public sealed record SystemDbCheckResponse(string Status, bool Database);

public sealed record SystemDatabaseProbeResult(bool Available, string? FailureCode, long? LatencyMs)
{
    public static SystemDatabaseProbeResult Ok(long latencyMs)
    {
        return new SystemDatabaseProbeResult(true, null, latencyMs);
    }

    public static SystemDatabaseProbeResult Unavailable(string failureCode)
    {
        return new SystemDatabaseProbeResult(false, failureCode, null);
    }
}

public sealed record SystemMigrationProbeResult(bool Available, string? FailureCode, long? AppliedCount, long? LatencyMs)
{
    public static SystemMigrationProbeResult Ok(long appliedCount, long latencyMs)
    {
        return new SystemMigrationProbeResult(true, null, appliedCount, latencyMs);
    }

    public static SystemMigrationProbeResult Unavailable(string failureCode)
    {
        return new SystemMigrationProbeResult(false, failureCode, null, null);
    }
}
