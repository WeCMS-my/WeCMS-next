using WeCms.Shared;

namespace WeCms.Modules.System.Settings;

public sealed class SettingService : ISettingService
{
    private const int MaxPageSize = 100;
    private const int MaxValueLength = 4000;
    private readonly ISettingRepository _repository;

    public SettingService(ISettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        var result = await _repository.ListAsync(new SettingListCriteria(page, pageSize, NormalizeOptional(query.Keyword, 80), NormalizeOptional(query.GroupCode, 80)), cancellationToken);
        return result with { Records = result.Records.Select(MaskSummary).ToArray() };
    }

    public async Task<SettingDetailDto> GetAsync(string key, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeRequired(key, "key", 120);
        var setting = await _repository.GetAsync(normalizedKey, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Setting was not found.");
        return MaskDetail(setting);
    }

    public async Task<SettingMutationResponse> UpdateAsync(string key, UpdateSettingRequest request, SettingRequestContext context, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeRequired(key, "key", 120);
        var setting = await _repository.GetAsync(normalizedKey, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Setting was not found.");
        ValidateValue(setting.ValueType, request.Value);

        await _repository.UpdateAsync(new SettingUpdateRecord(normalizedKey, NormalizeOptional(request.Value, MaxValueLength), context.ActorUserId, context.Now), cancellationToken);
        if (setting.IsSensitive)
        {
            await _repository.RecordAuditAsync(new SettingAuditRecord(context.ActorUserId, context.ActorUsername, "update-sensitive", normalizedKey, context.Ip, context.UserAgent, context.TraceId, "success", "Sensitive setting updated.", context.Now), cancellationToken);
        }

        return new SettingMutationResponse(normalizedKey);
    }

    private static SettingSummaryDto MaskSummary(SettingSummaryDto setting)
    {
        return setting.IsSensitive ? setting with { Value = null } : setting;
    }

    private static SettingDetailDto MaskDetail(SettingDetailDto setting)
    {
        return setting.IsSensitive ? setting with { Value = null } : setting;
    }

    private static void ValidateValue(string valueType, string? value)
    {
        _ = valueType switch
        {
            "string" or "number" or "boolean" or "json" => valueType,
            _ => throw Validation("valueType must be string, number, boolean, or json.")
        };

        if (value is not null && value.Length > MaxValueLength)
        {
            throw Validation($"value must be {MaxValueLength} characters or fewer.");
        }
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
