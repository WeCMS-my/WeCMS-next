namespace WeCms.Modules.System.System;

public sealed record HealthLiveResponse(string Status, DateTimeOffset Timestamp);

public sealed record HealthReadyResponse(string Status, bool DatabaseReady, long? DatabaseLatencyMs);

public sealed record SystemPingResponse(string Status, string Timezone, DateTimeOffset ServerTime);

public sealed record SystemVersionResponse(string Version, string Environment, string Framework);

public sealed record DbCheckResponse(string Status, string Database, string? Error);

public sealed record SecurePingResponse(string Status, DateTimeOffset Timestamp);
