using WeCms.Shared;

namespace WeCms.Modules.Organization.Positions;

public sealed class PositionService : IPositionService
{
    private const int MaxPageSize = 100;
    private readonly IPositionRepository _repository;

    public PositionService(IPositionRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        var status = NormalizeOptional(query.Status, 32);
        if (status is not null && status is not "enabled" and not "disabled")
        {
            throw Validation("status must be enabled or disabled.");
        }

        return _repository.ListAsync(new PositionListCriteria(page, pageSize, NormalizeOptional(query.Keyword, 80), status), cancellationToken);
    }

    public async Task<PositionDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Position was not found.");
    }

    public async Task<PositionMutationResponse> CreateAsync(CreatePositionRequest request, PositionRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 80);
        var name = NormalizeRequired(request.Name, "name", 120);
        var status = NormalizeStatus(request.Status);
        if (await _repository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "position code already exists.");
        }

        var id = await _repository.CreateAsync(new PositionCreateRecord(code, name, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "create", id, "success", "Position created.", cancellationToken);
        return new PositionMutationResponse(id);
    }

    public async Task<PositionMutationResponse> UpdateAsync(long id, UpdatePositionRequest request, PositionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var name = NormalizeRequired(request.Name, "name", 120);
        var status = NormalizeStatus(request.Status);
        await _repository.UpdateAsync(new PositionUpdateRecord(id, name, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "update", id, "success", "Position updated.", cancellationToken);
        return new PositionMutationResponse(id);
    }

    public async Task DeleteAsync(long id, PositionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        if (await _repository.HasUsersAsync(id, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Position assigned to users cannot be deleted.");
        }

        await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "success", "Position deleted.", cancellationToken);
    }

    public async Task EnableAsync(long id, PositionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetStatusAsync(id, "enabled", context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "Position enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, PositionRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetStatusAsync(id, "disabled", context.Now, cancellationToken);
        await AuditAsync(context, "disable", id, "success", "Position disabled.", cancellationToken);
    }

    private Task AuditAsync(PositionRequestContext context, string action, long targetPositionId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new PositionAuditRecord(context.ActorUserId, context.ActorUsername, action, targetPositionId, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now), cancellationToken);
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value, "status", 32);
        return normalized is "enabled" or "disabled" ? normalized : throw Validation("status must be enabled or disabled.");
    }

    private static string NormalizeRequired(string? value, string name, int maxLength) => NormalizeOptional(value, maxLength) ?? throw Validation($"{name} is required.");

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
