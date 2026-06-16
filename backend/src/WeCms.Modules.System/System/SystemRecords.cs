namespace WeCms.Modules.System.System;

public sealed record SystemLiveResponse(string Status);

public sealed record SystemReadyResponse(string Status, bool Database);

public sealed record SystemPingResponse(string Status);

public sealed record SystemVersionResponse(string Version);

public sealed record SystemDbCheckResponse(string Status, bool Database);

public sealed record SystemDatabaseProbeResult(bool Available, string? FailureCode)
{
    public static SystemDatabaseProbeResult Ok { get; } = new(true, null);

    public static SystemDatabaseProbeResult Unavailable(string failureCode)
    {
        return new SystemDatabaseProbeResult(false, failureCode);
    }
}
