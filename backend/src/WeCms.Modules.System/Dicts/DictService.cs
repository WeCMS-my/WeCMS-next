using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Modules.System.Dicts;

public sealed class DictService : IDictService
{
    private const int MaxPageSize = 100;
    private readonly IDictRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DictService(IDictRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        var status = NormalizeOptional(query.Status, 32);
        if (status is not null && status is not "enabled" and not "disabled")
        {
            throw Validation("status must be enabled or disabled.");
        }

        return _repository.ListTypesAsync(new DictTypeListCriteria(page, pageSize, NormalizeOptional(query.Keyword, 80), status), cancellationToken);
    }

    public async Task<DictTypeDetailDto> GetTypeAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetTypeAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Dictionary type was not found.");
    }

    public async Task<DictMutationResponse> CreateTypeAsync(CreateDictTypeRequest request, DictRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 80);
        var name = NormalizeRequired(request.Name, "name", 120);
        var description = NormalizeOptional(request.Description, 500);
        var status = NormalizeStatus(request.Status);
        if (await _repository.TypeCodeExistsAsync(code, null, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "dict type code already exists.");
        }

        var id = await _repository.CreateTypeAsync(new DictTypeCreateRecord(code, name, description, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "create-type", id, "dict-type", "success", "Dictionary type created.", cancellationToken);
        return new DictMutationResponse(id);
    }

    public async Task<DictMutationResponse> UpdateTypeAsync(long id, UpdateDictTypeRequest request, DictRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetTypeAsync(id, cancellationToken);
        var name = NormalizeRequired(request.Name, "name", 120);
        var description = NormalizeOptional(request.Description, 500);
        var status = NormalizeStatus(request.Status);
        await _repository.UpdateTypeAsync(new DictTypeUpdateRecord(id, name, description, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "update-type", id, "dict-type", "success", "Dictionary type updated.", cancellationToken);
        return new DictMutationResponse(id);
    }

    public Task EnableTypeAsync(long id, DictRequestContext context, CancellationToken cancellationToken)
    {
        return SetTypeStatusAsync(id, "enabled", false, context, cancellationToken);
    }

    public Task DisableTypeAsync(long id, DisableDictTypeRequest request, DictRequestContext context, CancellationToken cancellationToken)
    {
        return SetTypeStatusAsync(id, "disabled", request.CascadeValues, context, cancellationToken);
    }

    public async Task DeleteTypeAsync(long id, DictRequestContext context, CancellationToken cancellationToken)
    {
        var type = await GetTypeAsync(id, cancellationToken);
        if (type.IsSystem)
        {
            throw new DomainException(ApiCodes.BusinessError, "System dictionary types cannot be deleted.");
        }

        if (await _repository.TypeHasValuesAsync(id, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Dictionary type with values cannot be deleted.");
        }

        await _repository.SoftDeleteTypeAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete-type", id, "dict-type", "success", "Dictionary type deleted.", cancellationToken);
    }

    public async Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken)
    {
        _ = await GetTypeByCodeAsync(typeCode, cancellationToken);
        return await _repository.ListValuesAsync(NormalizeRequired(typeCode, "typeCode", 80), cancellationToken);
    }

    public async Task<DictMutationResponse> CreateValueAsync(string typeCode, CreateDictValueRequest request, DictRequestContext context, CancellationToken cancellationToken)
    {
        var type = await GetTypeByCodeAsync(typeCode, cancellationToken);
        var label = NormalizeRequired(request.Label, "label", 120);
        var value = NormalizeRequired(request.Value, "value", 160);
        var description = NormalizeOptional(request.Description, 500);
        var status = NormalizeStatus(request.Status);
        if (await _repository.ValueExistsAsync(type.Id, value, null, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "dict value already exists in this type.");
        }

        var id = await _repository.CreateValueAsync(new DictValueCreateRecord(type.Id, label, value, description, request.SortOrder, request.IsDefault, status, context.Now), cancellationToken);
        await AuditAsync(context, "create-value", id, "dict-value", "success", "Dictionary value created.", cancellationToken);
        return new DictMutationResponse(id);
    }

    public async Task<DictMutationResponse> UpdateValueAsync(long id, UpdateDictValueRequest request, DictRequestContext context, CancellationToken cancellationToken)
    {
        var current = await _repository.GetValueAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Dictionary value was not found.");
        var label = NormalizeRequired(request.Label, "label", 120);
        var value = NormalizeRequired(request.Value, "value", 160);
        var description = NormalizeOptional(request.Description, 500);
        var status = NormalizeStatus(request.Status);
        if (await _repository.ValueExistsAsync(current.TypeId, value, id, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "dict value already exists in this type.");
        }

        await _repository.UpdateValueAsync(new DictValueUpdateRecord(id, label, value, description, request.SortOrder, request.IsDefault, status, context.Now), cancellationToken);
        await AuditAsync(context, "update-value", id, "dict-value", "success", "Dictionary value updated.", cancellationToken);
        return new DictMutationResponse(id);
    }

    public Task EnableValueAsync(long id, DictRequestContext context, CancellationToken cancellationToken)
    {
        return SetValueStatusAsync(id, "enabled", context, cancellationToken);
    }

    public Task DisableValueAsync(long id, DictRequestContext context, CancellationToken cancellationToken)
    {
        return SetValueStatusAsync(id, "disabled", context, cancellationToken);
    }

    public async Task DeleteValueAsync(long id, DictRequestContext context, CancellationToken cancellationToken)
    {
        _ = await _repository.GetValueAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Dictionary value was not found.");
        await _repository.SoftDeleteValueAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete-value", id, "dict-value", "success", "Dictionary value deleted.", cancellationToken);
    }

    private async Task<DictTypeDetailDto> GetTypeByCodeAsync(string typeCode, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(typeCode, "typeCode", 80);
        return await _repository.GetTypeByCodeAsync(code, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Dictionary type was not found.");
    }

    private async Task SetTypeStatusAsync(long id, string status, bool cascadeValues, DictRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetTypeAsync(id, cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.SetTypeStatusAsync(id, status, context.Now, cancellationToken);
            if (status == "disabled" && cascadeValues)
            {
                await _repository.DisableValuesByTypeAsync(id, context.Now, cancellationToken);
            }

            var action = status == "enabled" ? "enable-type" : "disable-type";
            var detail = cascadeValues ? "Dictionary type disabled with values." : $"Dictionary type {status}.";
            await AuditAsync(context, action, id, "dict-type", "success", detail, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SetValueStatusAsync(long id, string status, DictRequestContext context, CancellationToken cancellationToken)
    {
        _ = await _repository.GetValueAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Dictionary value was not found.");
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.SetValueStatusAsync(id, status, context.Now, cancellationToken);
            var action = status == "enabled" ? "enable-value" : "disable-value";
            await AuditAsync(context, action, id, "dict-value", "success", $"Dictionary value {status}.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private Task AuditAsync(DictRequestContext context, string action, long targetId, string resource, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new DictAuditRecord(context.ActorUserId, context.ActorUsername, action, targetId, resource, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now), cancellationToken);
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
