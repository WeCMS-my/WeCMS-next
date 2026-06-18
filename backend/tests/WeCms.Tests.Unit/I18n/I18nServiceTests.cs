using WeCms.Modules.System.I18n;
using WeCms.Shared;

namespace WeCms.Tests.Unit.I18n;

public sealed class I18nServiceTests
{
    private static readonly I18nRequestContext Context = new(7, "admin", "127.0.0.1", "unit-test", "trace-i18n", DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task CreateAsync_RejectsDuplicateLocaleAndMessageKey()
    {
        var repository = new FakeI18nRepository { MessageExists = true };
        var service = new I18nMessageService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateI18nMessageRequest("zh-CN", "system", "system.dashboard.title", "工作台", null, "enabled"),
            Context,
            CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(0, repository.AuditCalls);
    }

    [Fact]
    public async Task GetPublicMessagesAsync_ReturnsOnlyEnabledMessagesForLocale()
    {
        var repository = new FakeI18nRepository();
        var service = new I18nMessageService(repository);

        var result = await service.GetPublicMessagesAsync(new PublicI18nMessagesQuery("en-US"), CancellationToken.None);

        Assert.Equal("Dashboard", result.Messages["system.dashboard.title"]);
        Assert.DoesNotContain("system.disabled", result.Messages.Keys);
        Assert.Equal("en-US", repository.LastPublicLocale);
        Assert.Equal("enabled", repository.LastPublicStatus);
    }

    [Fact]
    public async Task SwitchLocaleAsync_RejectsUnsupportedLocale()
    {
        var service = new I18nMessageService(new FakeI18nRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.SwitchLocaleAsync(
            new SwitchAccountLocaleRequest("fr-FR"),
            Context,
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private sealed class FakeI18nRepository : II18nMessageRepository
    {
        public bool MessageExists { get; init; }
        public int CreateCalls { get; private set; }
        public int AuditCalls { get; private set; }
        public string? LastPublicLocale { get; private set; }
        public string? LastPublicStatus { get; private set; }

        public Task<PagedResult<I18nMessageSummaryDto>> ListAsync(I18nMessageListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<I18nMessageSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<I18nMessageDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<I18nMessageDetailDto?>(new I18nMessageDetailDto(id, "zh-CN", "system", "system.dashboard.title", "工作台", null, "enabled", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        }

        public Task<bool> ExistsAsync(string locale, string messageKey, long? exceptId, CancellationToken cancellationToken)
        {
            return Task.FromResult(MessageExists);
        }

        public Task<long> CreateAsync(I18nMessageCreateRecord record, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(12L);
        }

        public Task UpdateAsync(I18nMessageUpdateRecord record, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<I18nPublicMessageRecord>> ListPublicMessagesAsync(string locale, string status, CancellationToken cancellationToken)
        {
            LastPublicLocale = locale;
            LastPublicStatus = status;
            return Task.FromResult<IReadOnlyList<I18nPublicMessageRecord>>(
            [
                new("system.dashboard.title", "Dashboard")
            ]);
        }

        public Task RecordAuditAsync(I18nAuditRecord record, CancellationToken cancellationToken)
        {
            AuditCalls++;
            return Task.CompletedTask;
        }
    }
}
