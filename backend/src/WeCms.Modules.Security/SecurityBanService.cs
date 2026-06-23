using WeCms.EventBus;
using WeCms.Modules.Security.Events;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Id;

namespace WeCms.Modules.Security;

public sealed class SecurityBanService : ISecurityBanService
{
    private const int MaxPageSize = 100;
    private const int MaxBatchUnbanCount = 50;
    private const int MaxTargetLength = 128;
    private const int MaxReasonLength = 500;
    private readonly ISecurityBanRepository _repository;
    private readonly ISecurityAlertService _alertService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IIdGenerator _idGenerator;
    private readonly ISecurityBanLookupCache _lookupCache;

    public SecurityBanService(
        ISecurityBanRepository repository,
        ISecurityAlertService alertService,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IIdGenerator idGenerator,
        ISecurityBanLookupCache lookupCache)
    {
        _repository = repository;
        _alertService = alertService;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _idGenerator = idGenerator;
        _lookupCache = lookupCache;
    }

    public Task<SecurityStatusDto> GetStatusAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _repository.GetStatusAsync(now, cancellationToken);
    }

    public Task<PagedResult<SecurityBanSummaryDto>> ListAsync(
        SecurityBanListQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than 0.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        var criteria = new SecurityBanListCriteria(
            page,
            pageSize,
            NormalizeOptionalBanType(query.BanType),
            NormalizeOptional(query.Target, MaxTargetLength),
            NormalizeOptional(query.Severity, 32),
            NormalizeOptional(query.Source, 64),
            query.ActiveOnly,
            DateTimeOffset.UtcNow);

        return _repository.ListAsync(criteria, cancellationToken);
    }

    public async Task<SecurityBanDetailDto> GetAsync(
        long id,
        CancellationToken cancellationToken)
    {
        EnsurePositiveId(id);
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Security ban was not found.");
    }

    public async Task<SecurityBanMutationResponse> UnbanAsync(
        long id,
        UnbanSecurityBanRequest request,
        SecurityBanRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        EnsurePositiveId(id);

        var reason = NormalizeRequired(request.Reason, "reason", MaxReasonLength);
        var ban = await GetAsync(id, cancellationToken);
        EnsureCanUnban(ban);
        await EnsureHighRiskUnbanAllowedAsync(ban, context, cancellationToken);

        await _repository.RevokeAsync(new SecurityBanRevokeRecord(id, context.ActorUserId, reason, context.Now), cancellationToken);
        await _lookupCache.RemoveAsync(ban.BanType, ban.Target, cancellationToken);
        await RecordAuditAsync(id, context, "unban", "success", $"Security ban {id} revoked.", cancellationToken);
        await RecordSecurityEventAsync("security.ban_unbanned", ban, context, $"Security ban {id} revoked.", cancellationToken);

        return new SecurityBanMutationResponse(id);
    }

    public async Task<BatchUnbanSecurityBansResponse> BatchUnbanAsync(
        BatchUnbanSecurityBansRequest request,
        SecurityBanRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var reason = NormalizeRequired(request.Reason, "reason", MaxReasonLength);
        var ids = NormalizeBatchIds(request.Ids);
        foreach (var id in ids)
        {
            var ban = await GetAsync(id, cancellationToken);
            EnsureCanUnban(ban);
            await EnsureHighRiskUnbanAllowedAsync(ban, context, cancellationToken);
            await _repository.RevokeAsync(new SecurityBanRevokeRecord(id, context.ActorUserId, reason, context.Now), cancellationToken);
            await _lookupCache.RemoveAsync(ban.BanType, ban.Target, cancellationToken);
            await RecordAuditAsync(id, context, "batch-unban", "success", $"Security ban {id} revoked in batch.", cancellationToken);
            await RecordSecurityEventAsync("security.ban_unbanned", ban, context, $"Security ban {id} revoked in batch.", cancellationToken);
        }

        return new BatchUnbanSecurityBansResponse(ids);
    }

    public async Task<SecurityBanMutationResponse> CreateTemporaryAsync(
        CreateSecurityBanRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        var normalized = new CreateSecurityBanRecord(
            NormalizeBanType(record.BanType),
            NormalizeRequired(record.Target, "target", MaxTargetLength),
            NormalizeRequired(record.Reason, "reason", MaxReasonLength),
            NormalizeSeverity(record.Severity),
            NormalizeRequired(record.Source, "source", 64),
            record.ExpiresAt,
            record.CreatedAt);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        long id;
        try
        {
            id = await _repository.CreateAsync(normalized, cancellationToken);
            await _outboxWriter.WriteAsync(
                new SecurityBanCreatedEvent(NewEventId(), normalized.CreatedAt, null, null, id, normalized.BanType, normalized.Target, normalized.Severity),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await _lookupCache.RemoveAsync(normalized.BanType, normalized.Target, cancellationToken);
        return new SecurityBanMutationResponse(id);
    }

    public async Task<SecurityBanRecord?> FindActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeBanType(banType);
        var normalizedTarget = NormalizeRequired(target, "target", MaxTargetLength);
        var cached = await _lookupCache.GetAsync(normalizedType, normalizedTarget, now, cancellationToken);
        if (cached is not null)
        {
            return cached.Record;
        }

        var active = await _repository.FindActiveAsync(normalizedType, normalizedTarget, now, cancellationToken);
        if (active is not null)
        {
            await _lookupCache.SetAsync(active, now, cancellationToken);
            return active;
        }

        await _lookupCache.SetMissAsync(normalizedType, normalizedTarget, now, cancellationToken);
        return null;
    }

    public async Task RecordHitAsync(
        SecurityBanRecord ban,
        SecurityBanHitContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ban);
        ArgumentNullException.ThrowIfNull(context);

        var securityEvent = new SecurityBanSecurityEventRecord(
            "security.ban_hit",
            context.UserId,
            context.Username,
            context.Ip,
            NormalizeSeverity(ban.Severity),
            "Security ban matched request.",
            context.Now,
            context.TraceId);

        await _repository.RecordSecurityEventAsync(securityEvent, cancellationToken);
        await _alertService.PublishIfRequiredAsync(
            SecurityAlertRecord.FromSecurityEvent(
                securityEvent.EventType,
                securityEvent.Severity,
                securityEvent.Message,
                securityEvent.TraceId,
                securityEvent.CreatedAt),
            cancellationToken);
    }

    private Task RecordAuditAsync(
        long id,
        SecurityBanRequestContext context,
        string action,
        string result,
        string detail,
        CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(
            new SecurityBanAuditRecord(
                context.ActorUserId,
                context.ActorUsername,
                action,
                id,
                context.Ip,
                context.UserAgent,
                context.TraceId,
                result,
                detail,
                context.Now),
            cancellationToken);
    }

    private async Task RecordSecurityEventAsync(
        string eventType,
        SecurityBanDetailDto ban,
        SecurityBanRequestContext context,
        string message,
        CancellationToken cancellationToken)
    {
        var securityEvent = new SecurityBanSecurityEventRecord(
            eventType,
            context.ActorUserId,
            context.ActorUsername,
            context.Ip,
            NormalizeSeverity(ban.Severity),
            message,
            context.Now,
            context.TraceId);

        await _repository.RecordSecurityEventAsync(securityEvent, cancellationToken);
        await _alertService.PublishIfRequiredAsync(
            SecurityAlertRecord.FromSecurityEvent(
                securityEvent.EventType,
                securityEvent.Severity,
                securityEvent.Message,
                securityEvent.TraceId,
                securityEvent.CreatedAt),
            cancellationToken);
    }

    private static void EnsureCanUnban(SecurityBanDetailDto ban)
    {
        if (ban.RevokedAt is not null)
        {
            throw new DomainException(ApiCodes.BusinessError, "Security ban is already revoked.");
        }
    }

    private Guid NewEventId() => Guid.ParseExact(_idGenerator.NewId(), "N");

    private async Task EnsureHighRiskUnbanAllowedAsync(
        SecurityBanDetailDto ban,
        SecurityBanRequestContext context,
        CancellationToken cancellationToken)
    {
        if (!IsCriticalSelfUserBan(ban, context.ActorUserId))
        {
            return;
        }

        if (!await _repository.UserHasRoleCodeAsync(context.ActorUserId, "super_admin", cancellationToken))
        {
            throw new DomainException(ApiCodes.Forbidden, "Only super_admin can unban a critical self-related security ban.");
        }
    }

    private static bool IsCriticalSelfUserBan(SecurityBanDetailDto ban, long actorUserId)
    {
        return string.Equals(ban.BanType, SecurityBanTypes.User, StringComparison.Ordinal)
            && string.Equals(ban.Severity, "critical", StringComparison.OrdinalIgnoreCase)
            && string.Equals(ban.Target, actorUserId.ToString(global::System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static IReadOnlyList<long> NormalizeBatchIds(IReadOnlyList<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count is 0 or > MaxBatchUnbanCount)
        {
            throw Validation($"ids must contain between 1 and {MaxBatchUnbanCount} values.");
        }

        var distinct = ids.Distinct().ToArray();
        foreach (var id in distinct)
        {
            EnsurePositiveId(id);
        }

        return distinct;
    }

    private static void EnsurePositiveId(long id)
    {
        if (id <= 0)
        {
            throw Validation("id must be greater than 0.");
        }
    }

    private static string NormalizeBanType(string value)
    {
        var normalized = NormalizeRequired(value, "banType", 32);
        if (!SecurityBanTypes.IsKnown(normalized))
        {
            throw new DomainException(ApiCodes.ValidationError, "banType is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalBanType(string? value)
    {
        var normalized = NormalizeOptional(value, 32);
        if (normalized is null)
        {
            return null;
        }

        if (!SecurityBanTypes.IsKnown(normalized))
        {
            throw Validation("banType is invalid.");
        }

        return normalized;
    }

    private static string NormalizeSeverity(string value)
    {
        return NormalizeRequired(value, "severity", 32);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw Validation($"value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException(ApiCodes.ValidationError, $"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static DomainException Validation(string message)
    {
        return new DomainException(ApiCodes.ValidationError, message);
    }
}
