using WeCms.Modules.Configuration;
using WeCms.Modules.Configuration.Events;
using WeCms.Modules.Configuration.I18n;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.I18n;

public sealed class I18nServiceTests
{
    private static readonly I18nRequestContext Context = new(7, "admin", "127.0.0.1", "unit-test", "trace-i18n", DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task CreateAsync_RejectsDuplicateLocaleAndMessageKey()
    {
        var repository = new FakeI18nRepository { MessageExists = true };
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateI18nMessageRequest("zh-CN", "system", "system.dashboard.title", "工作台", null, "enabled"),
            Context,
            CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(0, repository.AuditCalls);
        Assert.Equal(0, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task GetPublicMessagesAsync_ReturnsOnlyEnabledMessagesForLocale()
    {
        var repository = new FakeI18nRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        var result = await service.GetPublicMessagesAsync(new PublicI18nMessagesQuery("en-US"), CancellationToken.None);

        Assert.Equal("Dashboard", result.Messages["system.dashboard.title"]);
        Assert.DoesNotContain("system.disabled", result.Messages.Keys);
        Assert.Equal("en-US", repository.LastPublicLocale);
        Assert.Equal("enabled", repository.LastPublicStatus);
        Assert.Equal(0, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task SwitchLocaleAsync_RejectsUnsupportedLocale()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(new FakeI18nRepository(), cacheInvalidator);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.SwitchLocaleAsync(
            new SwitchAccountLocaleRequest("fr-FR"),
            Context,
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(0, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task CreateAsync_InvalidatesI18nCache()
    {
        var repository = new FakeI18nRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        var result = await service.CreateAsync(
            new CreateI18nMessageRequest("zh-CN", "system", "system.dashboard.title", "工作台", null, "enabled"),
            Context,
            CancellationToken.None);

        Assert.Equal(12, result.Id);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(1, repository.AuditCalls);
        Assert.Equal(1, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task CreateAsync_WritesI18nChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new WeCms.Tests.Unit.RecordingOutboxWriter(operations);
        var service = CreateService(new FakeI18nRepository(), unitOfWork: new FakeUnitOfWork(operations), outboxWriter: outbox);

        await service.CreateAsync(
            new CreateI18nMessageRequest("zh-CN", "system", "system.dashboard.title", "工作台", null, "enabled"),
            Context,
            CancellationToken.None);

        var evt = Assert.IsType<I18nChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(I18nChangedEvent.EventType, evt.Type);
        Assert.Equal(12, evt.MessageId);
        Assert.Equal("zh-CN", evt.Locale);
        Assert.Equal("system.dashboard.title", evt.MessageKey);
        AssertWriteWasInsideTransaction(operations, I18nChangedEvent.EventType);
    }

    [Fact]
    public async Task I18nChangedEvent_EvictsI18nCache()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var handler = new I18nChangedCacheHandler(cacheInvalidator);

        await handler.HandleAsync(new I18nChangedEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), Context.Now, "trace", null, 12, "zh-CN", "system.dashboard.title"), CancellationToken.None);

        Assert.Equal(1, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesI18nCache()
    {
        var repository = new FakeI18nRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        var result = await service.UpdateAsync(
            12,
            new UpdateI18nMessageRequest("system", "工作台", null, "enabled"),
            Context,
            CancellationToken.None);

        Assert.Equal(12, result.Id);
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal(1, repository.AuditCalls);
        Assert.Equal(1, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesI18nCache()
    {
        var repository = new FakeI18nRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        await service.DeleteAsync(12, Context, CancellationToken.None);

        Assert.Equal(1, repository.DeleteCalls);
        Assert.Equal(1, repository.AuditCalls);
        Assert.Equal(1, cacheInvalidator.I18nInvalidations);
    }

    [Fact]
    public async Task SwitchLocaleAsync_PreservesAccountPolicyWithoutInvalidatingMessageCache()
    {
        var repository = new FakeI18nRepository();
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(repository, cacheInvalidator);

        var result = await service.SwitchLocaleAsync(new SwitchAccountLocaleRequest("ms-MY"), Context, CancellationToken.None);

        Assert.Equal("ms-MY", result.Locale);
        Assert.Equal(1, repository.AuditCalls);
        Assert.Equal(0, cacheInvalidator.I18nInvalidations);
    }

    private static I18nMessageService CreateService(
        FakeI18nRepository repository,
        FakeConfigurationCacheInvalidator? cacheInvalidator = null,
        FakeUnitOfWork? unitOfWork = null,
        WeCms.Tests.Unit.RecordingOutboxWriter? outboxWriter = null)
    {
        return new I18nMessageService(
            repository,
            cacheInvalidator ?? new FakeConfigurationCacheInvalidator(),
            unitOfWork ?? new FakeUnitOfWork(),
            outboxWriter ?? new WeCms.Tests.Unit.RecordingOutboxWriter(),
            new WeCms.Tests.Unit.FixedTestIdGenerator());
    }

    private static void AssertWriteWasInsideTransaction(IReadOnlyList<string> operations, string eventType)
    {
        var orderedOperations = operations.ToList();
        var begin = orderedOperations.IndexOf("begin");
        var outbox = orderedOperations.IndexOf($"outbox:{eventType}");
        var commit = orderedOperations.IndexOf("commit");

        Assert.True(begin >= 0, string.Join(", ", operations));
        Assert.True(outbox > begin, string.Join(", ", operations));
        Assert.True(commit > outbox, string.Join(", ", operations));
    }

    private sealed class FakeI18nRepository : II18nMessageRepository
    {
        public bool MessageExists { get; init; }
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }
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
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
        {
            DeleteCalls++;
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

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly List<string>? _operations;

        public FakeUnitOfWork(List<string>? operations = null)
        {
            _operations = operations;
        }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _operations?.Add("begin");
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(_operations));
        }

        private sealed class FakeTransactionContext(List<string>? operations) : ITransactionContext
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                operations?.Add("commit");
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                operations?.Add("rollback");
                return Task.CompletedTask;
            }
        }
    }

    private sealed class FakeConfigurationCacheInvalidator : IConfigurationCacheInvalidator
    {
        public int I18nInvalidations { get; private set; }

        public Task InvalidateSettingsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task InvalidateDictsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task InvalidateI18nAsync(CancellationToken cancellationToken)
        {
            I18nInvalidations++;
            return Task.CompletedTask;
        }
    }
}
