namespace WeCms.Shared;

public sealed record PongResponse(string Ping);
public sealed record VersionResponse(string Version, string Runtime);
public sealed record DbCheckResponse(string Status, string Database);
public sealed record HealthReadyResponse(string Status, string Database);
public sealed record HealthLiveResponse(string Status, DateTimeOffset Timestamp);
public sealed record IdResponse(long Id);
public sealed record SyncResultResponse(int Synced);
