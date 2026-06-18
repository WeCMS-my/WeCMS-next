using WeCms.Shared;
using WeCms.Shared.Security;
using System.Net;

namespace WeCms.Modules.System.Settings;

public sealed class SettingService : ISettingService
{
    private const int MaxPageSize = 100;
    private const int MaxValueLength = 4000;
    private readonly ISettingRepository _repository;
    private readonly ISettingDefinitionProvider _definitions;
    private readonly ISettingSecretProtector _secretProtector;
    private readonly IIpRuleMatcher _ipRuleMatcher;
    private readonly ISettingCache _cache;

    public SettingService(
        ISettingRepository repository,
        ISettingDefinitionProvider definitions,
        ISettingSecretProtector secretProtector,
        IIpRuleMatcher ipRuleMatcher,
        ISettingCache cache)
    {
        _repository = repository;
        _definitions = definitions;
        _secretProtector = secretProtector;
        _ipRuleMatcher = ipRuleMatcher;
        _cache = cache;
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
        var definition = GetDefinition(normalizedKey);
        if (definition.IsReadonly)
        {
            throw new DomainException(ApiCodes.BusinessError, "Readonly setting cannot be updated.");
        }

        ValidateValue(setting.ValueType, request.Value);
        if (IsIpRulesSetting(normalizedKey))
        {
            ValidateIpRulesCore(request.Value ?? string.Empty);
        }

        var normalizedValue = NormalizeOptional(request.Value, MaxValueLength);
        var storedValue = definition.IsSensitive && normalizedValue is not null
            ? _secretProtector.Protect(normalizedValue)
            : normalizedValue;
        await _repository.UpdateAsync(new SettingUpdateRecord(normalizedKey, storedValue, context.ActorUserId, context.Now), cancellationToken);
        if (IsSensitive(normalizedKey, setting.IsSensitive))
        {
            await _repository.RecordAuditAsync(new SettingAuditRecord(context.ActorUserId, context.ActorUsername, "update-sensitive", normalizedKey, context.Ip, context.UserAgent, context.TraceId, "success", "Sensitive setting updated.", context.Now), cancellationToken);
        }
        else
        {
            await _repository.RecordAuditAsync(new SettingAuditRecord(context.ActorUserId, context.ActorUsername, "update", normalizedKey, context.Ip, context.UserAgent, context.TraceId, "success", "Setting updated.", context.Now), cancellationToken);
        }

        await _cache.RefreshAsync(cancellationToken);
        if (definition.IsSecuritySensitive)
        {
            await _repository.RecordSecurityEventAsync(new SettingSecurityEventRecord("security.setting_changed", context.ActorUserId, context.ActorUsername, context.Ip, "warning", $"Security setting {normalizedKey} changed.", context.Now, context.TraceId), cancellationToken);
        }

        return new SettingMutationResponse(normalizedKey);
    }

    public async Task<ValidateIpRulesResponse> ValidateIpRulesAsync(ValidateIpRulesRequest request, SettingRequestContext context, CancellationToken cancellationToken)
    {
        var response = ValidateIpRulesCore(request.Rules);
        await _repository.RecordAuditAsync(new SettingAuditRecord(context.ActorUserId, context.ActorUsername, "validate-ip-rules", "settings", context.Ip, context.UserAgent, context.TraceId, "success", "IP rules validated.", context.Now), cancellationToken);
        return response;
    }

    private ValidateIpRulesResponse ValidateIpRulesCore(string? rulesValue)
    {
        var rules = NormalizeOptional(rulesValue, MaxValueLength) ?? string.Empty;
        try
        {
            _ = _ipRuleMatcher.IsMatch(rules, IPAddress.Loopback);
            _ = _ipRuleMatcher.IsMatch(rules, IPAddress.IPv6Loopback);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation(exception.Message);
        }

        return new ValidateIpRulesResponse(true);
    }

    public async Task ReloadCacheAsync(SettingRequestContext context, CancellationToken cancellationToken)
    {
        await _cache.RefreshAsync(cancellationToken);
        await _repository.RecordAuditAsync(new SettingAuditRecord(context.ActorUserId, context.ActorUsername, "reload-cache", "settings", context.Ip, context.UserAgent, context.TraceId, "success", "Setting cache reloaded.", context.Now), cancellationToken);
    }

    private SettingDefinition GetDefinition(string key)
    {
        return _definitions.Find(key) ?? throw Validation("setting key is not defined.");
    }

    private static bool IsIpRulesSetting(string key)
    {
        return key is "security.ipAllowRules" or "security.ipDenyRules";
    }

    private SettingSummaryDto MaskSummary(SettingSummaryDto setting)
    {
        return IsSensitive(setting.Key, setting.IsSensitive) ? setting with { Value = null, IsSensitive = true } : setting;
    }

    private SettingDetailDto MaskDetail(SettingDetailDto setting)
    {
        return IsSensitive(setting.Key, setting.IsSensitive) ? setting with { Value = null, IsSensitive = true } : setting;
    }

    private bool IsSensitive(string key, bool rowIsSensitive)
    {
        return rowIsSensitive || _definitions.Find(key)?.IsSensitive == true;
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
