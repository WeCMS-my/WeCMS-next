using WeCms.Modules.Configuration;
using WeCms.Modules.Configuration.Events;
using WeCms.Modules.Configuration.Settings;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Settings;

public sealed class SettingServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = CreateService(new FakeSettingRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(new SettingListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetAsync_MasksSensitiveValue()
    {
        var service = CreateService(new FakeSettingRepository { Sensitive = true, Value = "secret" });

        var detail = await service.GetAsync("site.secret", CancellationToken.None);

        Assert.True(detail.IsSensitive);
        Assert.Null(detail.Value);
    }

    [Fact]
    public async Task GetAsync_MasksDefinitionSensitiveValueWhenRowFlagIsFalse()
    {
        var service = CreateService(new FakeSettingRepository { Key = "jwt_secret", Sensitive = false, Value = "secret" });

        var detail = await service.GetAsync("jwt_secret", CancellationToken.None);

        Assert.True(detail.IsSensitive);
        Assert.Null(detail.Value);
    }

    [Fact]
    public async Task ListAsync_MasksDefinitionSensitiveValueWhenRowFlagIsFalse()
    {
        var service = CreateService(new FakeSettingRepository { Key = "jwt_secret", Sensitive = false, Value = "secret" });

        var result = await service.ListAsync(new SettingListQuery(), CancellationToken.None);

        var setting = Assert.Single(result.Records);
        Assert.True(setting.IsSensitive);
        Assert.Null(setting.Value);
    }

    [Fact]
    public async Task UpdateAsync_RecordsAuditForSensitiveSetting()
    {
        var repository = new FakeSettingRepository { Key = "security.passwordPepper", Sensitive = false };
        var service = CreateService(repository);

        await service.UpdateAsync("security.passwordPepper", new UpdateSettingRequest("changed"), Context(), CancellationToken.None);

        Assert.Equal("update-sensitive", repository.AuditAction);
        Assert.True(repository.AuditRecorded);
    }

    [Fact]
    public async Task UpdateAsync_RejectsMissingSetting()
    {
        var service = CreateService(new FakeSettingRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync("missing", new UpdateSettingRequest("value"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUndefinedSetting()
    {
        var service = CreateService(new FakeSettingRepository { Key = "unknown.key" });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync("unknown.key", new UpdateSettingRequest("value"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_RejectsReadonlySetting()
    {
        var service = CreateService(new FakeSettingRepository { Key = "auth_key" });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync("auth_key", new UpdateSettingRequest("value"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_ProtectsSensitiveValueAndWritesSecurityEvent()
    {
        var repository = new FakeSettingRepository { Key = "security.passwordPepper", Sensitive = true };
        var protector = new FakeSettingSecretProtector();
        var service = CreateService(repository, protector: protector);

        await service.UpdateAsync("security.passwordPepper", new UpdateSettingRequest("plain-secret"), Context(), CancellationToken.None);

        Assert.Equal("protected:plain-secret", repository.UpdatedValue);
        Assert.Equal(1, repository.SecurityEventCount);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesSettingsCache()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var service = CreateService(new FakeSettingRepository(), cacheInvalidator: cacheInvalidator);

        await service.UpdateAsync("security.passwordPepper", new UpdateSettingRequest("changed"), Context(), CancellationToken.None);

        Assert.Equal(1, cacheInvalidator.SettingsInvalidationCalls);
    }

    [Fact]
    public async Task UpdateAsync_WritesSettingChangedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new WeCms.Tests.Unit.RecordingOutboxWriter(operations);
        var service = CreateService(new FakeSettingRepository(), unitOfWork: new FakeUnitOfWork(operations), outboxWriter: outbox);

        await service.UpdateAsync("security.passwordPepper", new UpdateSettingRequest("changed"), Context(), CancellationToken.None);

        var evt = Assert.IsType<SettingChangedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(SettingChangedEvent.EventType, evt.Type);
        Assert.Equal("security.passwordPepper", evt.SettingKey);
        AssertWriteWasInsideTransaction(operations, SettingChangedEvent.EventType);
    }

    [Fact]
    public async Task SettingChangedEvent_EvictsSettingCache()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var handler = new SettingChangedCacheHandler(cacheInvalidator);

        await handler.HandleAsync(new SettingChangedEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), Context().Now, "trace", null, "security.passwordPepper"), CancellationToken.None);

        Assert.Equal(1, cacheInvalidator.SettingsInvalidationCalls);
    }

    [Fact]
    public async Task ValidateIpRulesAsync_AcceptsExactCidrAndIpv6Rules()
    {
        var service = CreateService(new FakeSettingRepository());

        var response = await service.ValidateIpRulesAsync(new ValidateIpRulesRequest("127.0.0.1, 10.0.0.0/8, ::1"), Context(), CancellationToken.None);

        Assert.True(response.Valid);
    }

    [Fact]
    public async Task ValidateIpRulesAsync_RejectsInvalidRule()
    {
        var service = CreateService(new FakeSettingRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ValidateIpRulesAsync(new ValidateIpRulesRequest("not-an-ip"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task ReloadCacheAsync_RefreshesCacheAndAudits()
    {
        var cacheInvalidator = new FakeConfigurationCacheInvalidator();
        var repository = new FakeSettingRepository();
        var service = CreateService(repository, cacheInvalidator: cacheInvalidator);

        await service.ReloadCacheAsync(Context(), CancellationToken.None);

        Assert.Equal(1, cacheInvalidator.SettingsInvalidationCalls);
        Assert.True(repository.AuditRecorded);
    }

    private static SettingRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private static SettingService CreateService(
        FakeSettingRepository repository,
        ISettingSecretProtector? protector = null,
        IConfigurationCacheInvalidator? cacheInvalidator = null,
        FakeUnitOfWork? unitOfWork = null,
        WeCms.Tests.Unit.RecordingOutboxWriter? outboxWriter = null)
    {
        return new SettingService(
            repository,
            new SettingDefinitionProvider(),
            protector ?? new FakeSettingSecretProtector(),
            new IpRuleMatcher(),
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

    private sealed class FakeSettingRepository : ISettingRepository
    {
        public bool Sensitive { get; init; }
        public bool Missing { get; init; }
        public string Key { get; init; } = "security.passwordPepper";
        public string Value { get; init; } = "current";
        public bool AuditRecorded { get; private set; }
        public string? AuditAction { get; private set; }
        public string? UpdatedValue { get; private set; }
        public int SecurityEventCount { get; private set; }

        public Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListCriteria criteria, CancellationToken cancellationToken)
        {
            var dto = new SettingSummaryDto(Key, Sensitive ? null : Value, "string", "security", "Setting", null, Sensitive, true, DateTimeOffset.UnixEpoch, null);
            return Task.FromResult(new PagedResult<SettingSummaryDto>([dto], criteria.Page, criteria.PageSize, 1));
        }

        public Task<SettingDetailDto?> GetAsync(string key, CancellationToken cancellationToken)
        {
            if (Missing)
            {
                return Task.FromResult<SettingDetailDto?>(null);
            }

            return Task.FromResult<SettingDetailDto?>(new SettingDetailDto(key, Value, "string", "security", "Setting", null, Sensitive, true, DateTimeOffset.UnixEpoch, null));
        }

        public Task UpdateAsync(SettingUpdateRecord record, CancellationToken cancellationToken)
        {
            UpdatedValue = record.Value;
            return Task.CompletedTask;
        }

        public Task RecordAuditAsync(SettingAuditRecord record, CancellationToken cancellationToken)
        {
            AuditRecorded = true;
            AuditAction = record.Action;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SettingSecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingSecretProtector : ISettingSecretProtector
    {
        public string Protect(string value) => $"protected:{value}";
        public string Unprotect(string protectedValue) => protectedValue.StartsWith("protected:", StringComparison.Ordinal) ? protectedValue["protected:".Length..] : protectedValue;
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
        public int SettingsInvalidationCalls { get; private set; }

        public Task InvalidateSettingsAsync(CancellationToken cancellationToken)
        {
            SettingsInvalidationCalls++;
            return Task.CompletedTask;
        }

        public Task InvalidateDictsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task InvalidateI18nAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
