using System.Text.RegularExpressions;
using WeCms.EventBus;
using WeCms.Modules.Configuration.Events;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Id;

namespace WeCms.Modules.Configuration.I18n;

public sealed partial class I18nMessageService : II18nMessageService
{
    private const int MaxPageSize = 100;
    private static readonly HashSet<string> SupportedLocales = new(StringComparer.Ordinal)
    {
        "zh-CN",
        "en-US",
        "ms-MY"
    };

    private readonly II18nMessageRepository _repository;
    private readonly IConfigurationCacheInvalidator _cacheInvalidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IIdGenerator _idGenerator;

    public I18nMessageService(
        II18nMessageRepository repository,
        IConfigurationCacheInvalidator cacheInvalidator,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _idGenerator = idGenerator;
    }

    public Task<PagedResult<I18nMessageSummaryDto>> ListAsync(I18nMessageListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        var locale = NormalizeOptional(query.Locale, 16);
        if (locale is not null)
        {
            EnsureLocale(locale);
        }

        var status = NormalizeOptional(query.Status, 32);
        if (status is not null)
        {
            EnsureStatus(status);
        }

        return _repository.ListAsync(
            new I18nMessageListCriteria(
                page,
                pageSize,
                locale,
                NormalizeOptional(query.Module, 80),
                NormalizeOptional(query.Keyword, 120),
                status),
            cancellationToken);
    }

    public async Task<I18nMessageDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "i18n message was not found.");
    }

    public async Task<I18nMutationResponse> CreateAsync(CreateI18nMessageRequest request, I18nRequestContext context, CancellationToken cancellationToken)
    {
        var locale = NormalizeLocale(request.Locale);
        var module = NormalizeRequired(request.Module, "module", 80);
        var messageKey = NormalizeMessageKey(request.MessageKey);
        var messageValue = NormalizeRequired(request.MessageValue, "messageValue", 4000);
        var remark = NormalizeOptional(request.Remark, 500);
        var status = NormalizeStatus(request.Status);

        if (await _repository.ExistsAsync(locale, messageKey, null, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "i18n message already exists for locale and key.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        long id;
        try
        {
            id = await _repository.CreateAsync(new I18nMessageCreateRecord(locale, module, messageKey, messageValue, remark, status, context.Now), cancellationToken);
            await AuditAsync(context, "create", id, "success", "i18n message created.", cancellationToken);
            await WriteChangedAsync(context, id, locale, messageKey, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await InvalidateI18nAsync(cancellationToken);
        return new I18nMutationResponse(id);
    }

    public async Task<I18nMutationResponse> UpdateAsync(long id, UpdateI18nMessageRequest request, I18nRequestContext context, CancellationToken cancellationToken)
    {
        var current = await GetAsync(id, cancellationToken);
        var module = NormalizeRequired(request.Module, "module", 80);
        var messageValue = NormalizeRequired(request.MessageValue, "messageValue", 4000);
        var remark = NormalizeOptional(request.Remark, 500);
        var status = NormalizeStatus(request.Status);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.UpdateAsync(new I18nMessageUpdateRecord(id, module, messageValue, remark, status, context.Now), cancellationToken);
            await AuditAsync(context, "update", id, "success", "i18n message updated.", cancellationToken);
            await WriteChangedAsync(context, id, current.Locale, current.MessageKey, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await InvalidateI18nAsync(cancellationToken);
        return new I18nMutationResponse(id);
    }

    public async Task DeleteAsync(long id, I18nRequestContext context, CancellationToken cancellationToken)
    {
        var current = await GetAsync(id, cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
            await AuditAsync(context, "delete", id, "success", "i18n message deleted.", cancellationToken);
            await WriteChangedAsync(context, id, current.Locale, current.MessageKey, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await InvalidateI18nAsync(cancellationToken);
    }

    public async Task<I18nMessagesResponse> GetPublicMessagesAsync(PublicI18nMessagesQuery query, CancellationToken cancellationToken)
    {
        var locale = NormalizeLocale(query.Locale);
        var rows = await _repository.ListPublicMessagesAsync(locale, "enabled", cancellationToken);
        return new I18nMessagesResponse(
            locale,
            rows.ToDictionary(row => row.MessageKey, row => row.MessageValue, StringComparer.Ordinal));
    }

    public async Task<AccountI18nSwitchResponse> SwitchLocaleAsync(SwitchAccountLocaleRequest request, I18nRequestContext context, CancellationToken cancellationToken)
    {
        var locale = NormalizeLocale(request.Locale);
        await AuditAsync(context, "switch-locale", null, "success", $"Account locale switched to {locale}.", cancellationToken);
        return new AccountI18nSwitchResponse(locale);
    }

    private Task AuditAsync(I18nRequestContext context, string action, long? targetId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(
            new I18nAuditRecord(context.ActorUserId, context.ActorUsername, action, targetId, "i18n-message", context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now),
            cancellationToken);
    }

    private Task WriteChangedAsync(I18nRequestContext context, long messageId, string locale, string messageKey, CancellationToken cancellationToken)
    {
        return _outboxWriter.WriteAsync(new I18nChangedEvent(NewEventId(), context.Now, context.TraceId, null, messageId, locale, messageKey), cancellationToken);
    }

    private Guid NewEventId() => Guid.ParseExact(_idGenerator.NewId(), "N");

    private Task InvalidateI18nAsync(CancellationToken cancellationToken) => _cacheInvalidator.InvalidateI18nAsync(cancellationToken);

    private static string NormalizeLocale(string value)
    {
        var locale = NormalizeRequired(value, "locale", 16);
        EnsureLocale(locale);
        return locale;
    }

    private static void EnsureLocale(string locale)
    {
        if (!SupportedLocales.Contains(locale))
        {
            throw Validation("locale must be zh-CN, en-US, or ms-MY.");
        }
    }

    private static string NormalizeStatus(string value)
    {
        var status = NormalizeRequired(value, "status", 32);
        EnsureStatus(status);
        return status;
    }

    private static void EnsureStatus(string status)
    {
        if (status is not ("enabled" or "disabled"))
        {
            throw Validation("status must be enabled or disabled.");
        }
    }

    private static string NormalizeMessageKey(string value)
    {
        var messageKey = NormalizeRequired(value, "messageKey", 160);
        return MessageKeyPattern().IsMatch(messageKey)
            ? messageKey
            : throw Validation("messageKey must contain lowercase letters, numbers, dots, underscores, or hyphens.");
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

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$")]
    private static partial Regex MessageKeyPattern();
}
