using WeCms.Shared;

namespace WeCms.Modules.System.Security;

public static class SecurityBanTypes
{
    public const string Ip = "ip";
    public const string User = "user";

    public static bool IsKnown(string value)
    {
        return value is Ip or User;
    }
}

public sealed record SecurityBanRecord(
    long Id,
    string BanType,
    string Target,
    string Reason,
    string Severity,
    string Source,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt);

public sealed record SecurityStatusDto(
    long ActiveBans,
    long ActiveIpBans,
    long ActiveUserBans,
    long CriticalActiveBans,
    DateTimeOffset GeneratedAt);

public sealed record SecurityBanListQuery(
    int Page = 1,
    int PageSize = 20,
    string? BanType = null,
    string? Target = null,
    string? Severity = null,
    string? Source = null,
    bool ActiveOnly = true);

public sealed record SecurityBanListCriteria(
    int Page,
    int PageSize,
    string? BanType,
    string? Target,
    string? Severity,
    string? Source,
    bool ActiveOnly,
    DateTimeOffset Now);

public sealed record SecurityBanSummaryDto(
    long Id,
    string BanType,
    string Target,
    string Reason,
    string Severity,
    string Source,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SecurityBanDetailDto(
    long Id,
    string BanType,
    string Target,
    string Reason,
    string Severity,
    string Source,
    DateTimeOffset? ExpiresAt,
    long? RevokedBy,
    DateTimeOffset? RevokedAt,
    string? RevokeReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long? CreatedBy,
    string? CreatedByUsername);

public sealed record UnbanSecurityBanRequest(string Reason);

public sealed record BatchUnbanSecurityBansRequest(IReadOnlyList<long> Ids, string Reason);

public sealed record SecurityBanMutationResponse(long Id);

public sealed record BatchUnbanSecurityBansResponse(IReadOnlyList<long> Ids);

public sealed record CreateSecurityBanRecord(
    string BanType,
    string Target,
    string Reason,
    string Severity,
    string Source,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record SecurityBanRequestContext(
    long ActorUserId,
    string ActorUsername,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset Now);

public sealed record SecurityBanRevokeRecord(
    long Id,
    long RevokedBy,
    string RevokeReason,
    DateTimeOffset Now);

public sealed record SecurityBanAuditRecord(
    long ActorUserId,
    string ActorUsername,
    string Action,
    long TargetBanId,
    string Ip,
    string UserAgent,
    string TraceId,
    string Result,
    string Detail,
    DateTimeOffset Now);

public sealed record SecurityBanHitContext(
    long? UserId,
    string? Username,
    string Ip,
    string UserAgent,
    string TraceId,
    DateTimeOffset Now);

public sealed record SecurityBanSecurityEventRecord(
    string EventType,
    long? UserId,
    string? Username,
    string Ip,
    string Severity,
    string Message,
    DateTimeOffset CreatedAt,
    string TraceId = "");

public interface ISecurityBanService
{
    Task<SecurityStatusDto> GetStatusAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PagedResult<SecurityBanSummaryDto>> ListAsync(
        SecurityBanListQuery query,
        CancellationToken cancellationToken);

    Task<SecurityBanDetailDto> GetAsync(
        long id,
        CancellationToken cancellationToken);

    Task<SecurityBanMutationResponse> UnbanAsync(
        long id,
        UnbanSecurityBanRequest request,
        SecurityBanRequestContext context,
        CancellationToken cancellationToken);

    Task<BatchUnbanSecurityBansResponse> BatchUnbanAsync(
        BatchUnbanSecurityBansRequest request,
        SecurityBanRequestContext context,
        CancellationToken cancellationToken);

    Task<SecurityBanMutationResponse> CreateTemporaryAsync(
        CreateSecurityBanRecord record,
        CancellationToken cancellationToken);

    Task<SecurityBanRecord?> FindActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RecordHitAsync(
        SecurityBanRecord ban,
        SecurityBanHitContext context,
        CancellationToken cancellationToken);
}

public interface ISecurityBanRepository
{
    Task<SecurityStatusDto> GetStatusAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PagedResult<SecurityBanSummaryDto>> ListAsync(
        SecurityBanListCriteria criteria,
        CancellationToken cancellationToken);

    Task<SecurityBanDetailDto?> GetAsync(
        long id,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        SecurityBanRevokeRecord record,
        CancellationToken cancellationToken);

    Task<bool> IsSuperAdminAsync(
        long userId,
        CancellationToken cancellationToken);

    Task RecordAuditAsync(
        SecurityBanAuditRecord record,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        CreateSecurityBanRecord record,
        CancellationToken cancellationToken);

    Task<SecurityBanRecord?> FindActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(
        SecurityBanSecurityEventRecord record,
        CancellationToken cancellationToken);
}
