using WeCms.Modules.Security.Events;
using WeCms.Modules.Security;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Security;

public sealed class SecurityBanServiceTests
{
    [Fact]
    public async Task FindActiveAsync_RejectsUnknownBanType()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.FindActiveAsync("device", "device-1", Now, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task FindActiveAsync_NormalizesTargetAndDelegatesToRepository()
    {
        var repository = new FakeSecurityBanRepository
        {
            ActiveBan = new SecurityBanRecord(7, SecurityBanTypes.Ip, "192.168.1.10", "bruteforce", "warning", "login", Now.AddMinutes(10), null)
        };
        var service = CreateService(repository);

        var ban = await service.FindActiveAsync(SecurityBanTypes.Ip, " 192.168.1.10 ", Now, CancellationToken.None);

        Assert.NotNull(ban);
        Assert.Equal(SecurityBanTypes.Ip, repository.LastBanType);
        Assert.Equal("192.168.1.10", repository.LastTarget);
    }

    [Fact]
    public async Task FindActiveAsync_CachesPositiveActiveBanLookup()
    {
        var repository = new FakeSecurityBanRepository
        {
            ActiveBan = new SecurityBanRecord(7, SecurityBanTypes.Ip, "192.168.1.10", "bruteforce", "warning", "login", Now.AddMinutes(10), null)
        };
        var cache = new FakeSecurityBanLookupCache();
        var service = CreateService(repository, cache: cache);

        var first = await service.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.10", Now, CancellationToken.None);
        var second = await service.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.10", Now, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, repository.FindActiveCalls);
        Assert.Equal(1, cache.SetCalls);
    }

    [Fact]
    public async Task FindActiveAsync_CachesNegativeMisses()
    {
        var repository = new FakeSecurityBanRepository();
        var cache = new FakeSecurityBanLookupCache();
        var service = CreateService(repository, cache: cache);

        var first = await service.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.10", Now, CancellationToken.None);
        var second = await service.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.10", Now, CancellationToken.None);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, repository.FindActiveCalls);
        Assert.Equal(0, cache.SetCalls);
        Assert.Equal(1, cache.SetMissCalls);
    }

    [Fact]
    public async Task UnbanAsync_InvalidatesActiveBanLookupCache()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.Ip, "192.168.1.10", "manual", "critical", "admin", null, null, null, null, Now, Now, null, null)
        };
        var cache = new FakeSecurityBanLookupCache();
        var service = CreateService(repository, cache: cache);

        await service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None);

        Assert.Equal(1, repository.RevokeCalls);
        Assert.Equal(1, cache.RemoveCalls);
        Assert.Contains("security:active-ban:", cache.LastRemovedKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateTemporaryAsync_InvalidatesActiveBanLookupCache()
    {
        var operations = new List<string>();
        var cache = new FakeSecurityBanLookupCache(operations);
        var outbox = new WeCms.Tests.Unit.RecordingOutboxWriter(operations);
        var service = CreateService(new FakeSecurityBanRepository(), cache: cache, unitOfWork: new FakeUnitOfWork(operations), outboxWriter: outbox);

        await service.CreateTemporaryAsync(
            new CreateSecurityBanRecord(SecurityBanTypes.Ip, "127.0.0.1", "bruteforce", "warning", "login", Now.AddMinutes(5), Now),
            CancellationToken.None);

        Assert.Equal(1, cache.RemoveCalls);
        Assert.True(operations.IndexOf("commit") < operations.IndexOf("cache-remove"), string.Join(", ", operations));
    }

    [Fact]
    public async Task SecurityBanService_UsesCacheAbstractionForActiveBanLookup()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Security", "SecurityBanService.cs"), TestContext.Current.CancellationToken);

        var cacheSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Security", "SecurityBanLookupCache.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("ISecurityBanLookupCache", source, StringComparison.Ordinal);
        Assert.Contains("_lookupCache.GetAsync", source, StringComparison.Ordinal);
        Assert.Contains("_lookupCache.SetAsync", source, StringComparison.Ordinal);
        Assert.Contains("_lookupCache.SetMissAsync", source, StringComparison.Ordinal);
        Assert.Contains("RemoveAsync", source, StringComparison.Ordinal);
        Assert.Contains("ICache", cacheSource, StringComparison.Ordinal);
        Assert.Contains("ICacheKeyBuilder", cacheSource, StringComparison.Ordinal);
        Assert.Contains("NegativeCacheTtl", cacheSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordHitAsync_WritesSecurityEvent()
    {
        var repository = new FakeSecurityBanRepository();
        var alertService = new FakeSecurityAlertService();
        var service = CreateService(repository, alertService);

        await service.RecordHitAsync(
            new SecurityBanRecord(7, SecurityBanTypes.User, "42", "admin reset", "critical", "manual", null, null),
            new SecurityBanHitContext(42, "admin", "192.168.1.10", "unit-test", "trace-ban", Now),
            CancellationToken.None);

        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("security.ban_hit", repository.LastSecurityEventType);
        Assert.Equal("critical", repository.LastSecurityEventSeverity);
        Assert.Equal(1, alertService.Count);
    }

    [Fact]
    public async Task ListAsync_RejectsOversizedPageSize()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new SecurityBanListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UnbanAsync_RequiresReason()
    {
        var service = CreateService(new FakeSecurityBanRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest(" "), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UnbanAsync_RejectsAlreadyRevokedBan()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.Ip, "192.168.1.10", "bruteforce", "warning", "login", null, 1, Now, "fixed", Now, Now, null, null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal(0, repository.RevokeCalls);
    }

    [Fact]
    public async Task UnbanAsync_WritesAuditAndSecurityEvent()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.User, "42", "manual", "critical", "admin", null, null, null, null, Now, Now, null, null)
        };
        var service = CreateService(repository);

        var response = await service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None);

        Assert.Equal(7, response.Id);
        Assert.Equal(1, repository.RevokeCalls);
        Assert.Equal(1, repository.AuditCount);
        Assert.Equal("unban", repository.LastAuditAction);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("security.ban_unbanned", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task UnbanAsync_RejectsCriticalSelfUserBanForNonSuperAdmin()
    {
        var repository = new FakeSecurityBanRepository
        {
            BanDetail = new SecurityBanDetailDto(7, SecurityBanTypes.User, "9", "manual", "critical", "admin", null, null, null, null, Now, Now, null, null),
            IsSuperAdmin = false
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UnbanAsync(7, new UnbanSecurityBanRequest("reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.Forbidden, exception.Code);
        Assert.Equal(0, repository.RevokeCalls);
    }

    [Fact]
    public async Task BatchUnbanAsync_RejectsTooManyIds()
    {
        var service = CreateService(new FakeSecurityBanRepository());
        var ids = Enumerable.Range(1, 51).Select(static value => (long)value).ToArray();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.BatchUnbanAsync(new BatchUnbanSecurityBansRequest(ids, "reviewed"), Context, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateTemporaryAsync_WritesSecurityBanCreatedEventBeforeCommit()
    {
        var operations = new List<string>();
        var outbox = new WeCms.Tests.Unit.RecordingOutboxWriter(operations);
        var service = CreateService(new FakeSecurityBanRepository(), unitOfWork: new FakeUnitOfWork(operations), outboxWriter: outbox);

        await service.CreateTemporaryAsync(
            new CreateSecurityBanRecord(SecurityBanTypes.Ip, "127.0.0.1", "bruteforce", "warning", "login", Now.AddMinutes(5), Now),
            CancellationToken.None);

        var evt = Assert.IsType<SecurityBanCreatedEvent>(Assert.Single(outbox.Events));
        Assert.Equal(SecurityBanCreatedEvent.EventType, evt.Type);
        Assert.Equal(11, evt.BanId);
        Assert.Equal(SecurityBanTypes.Ip, evt.BanType);
        Assert.Equal("127.0.0.1", evt.Target);
        Assert.Equal("warning", evt.Severity);
        AssertWriteWasInsideTransaction(operations, SecurityBanCreatedEvent.EventType);
    }

    private static readonly DateTimeOffset Now = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    private static readonly SecurityBanRequestContext Context = new(9, "operator", "127.0.0.1", "unit-test", "trace-security", Now);

    private static SecurityBanService CreateService(
        ISecurityBanRepository repository,
        FakeSecurityAlertService? alertService = null,
        FakeUnitOfWork? unitOfWork = null,
        WeCms.Tests.Unit.RecordingOutboxWriter? outboxWriter = null,
        ISecurityBanLookupCache? cache = null)
    {
        return new SecurityBanService(
            repository,
            alertService ?? new FakeSecurityAlertService(),
            unitOfWork ?? new FakeUnitOfWork(),
            outboxWriter ?? new WeCms.Tests.Unit.RecordingOutboxWriter(),
            new WeCms.Tests.Unit.FixedTestIdGenerator(),
            cache ?? new FakeSecurityBanLookupCache());
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
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

    private sealed class FakeSecurityBanRepository : ISecurityBanRepository
    {
        public SecurityBanRecord? ActiveBan { get; init; }

        public SecurityBanDetailDto? BanDetail { get; init; }

        public string LastBanType { get; private set; } = string.Empty;

        public string LastTarget { get; private set; } = string.Empty;

        public int SecurityEventCount { get; private set; }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public string LastSecurityEventSeverity { get; private set; } = string.Empty;

        public int RevokeCalls { get; private set; }

        public int FindActiveCalls { get; private set; }

        public int AuditCount { get; private set; }

        public string LastAuditAction { get; private set; } = string.Empty;

        public bool IsSuperAdmin { get; init; } = true;

        public Task<SecurityBanRecord?> FindActiveAsync(string banType, string target, DateTimeOffset now, CancellationToken cancellationToken)
        {
            FindActiveCalls++;
            LastBanType = banType;
            LastTarget = target;
            return Task.FromResult(ActiveBan);
        }

        public Task<SecurityStatusDto> GetStatusAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SecurityStatusDto(0, 0, 0, 0, now));
        }

        public Task<PagedResult<SecurityBanSummaryDto>> ListAsync(SecurityBanListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<SecurityBanSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<SecurityBanDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult(BanDetail);
        }

        public Task RevokeAsync(SecurityBanRevokeRecord record, CancellationToken cancellationToken)
        {
            RevokeCalls++;
            return Task.CompletedTask;
        }

        public Task<long> CreateAsync(CreateSecurityBanRecord record, CancellationToken cancellationToken)
        {
            return Task.FromResult(11L);
        }

        public Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsSuperAdmin);
        }

        public Task RecordAuditAsync(SecurityBanAuditRecord record, CancellationToken cancellationToken)
        {
            AuditCount++;
            LastAuditAction = record.Action;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityBanSecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            LastSecurityEventSeverity = record.Severity;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityBanLookupCache : ISecurityBanLookupCache
    {
        private readonly Dictionary<string, SecurityBanLookupCacheResult> _values = new(StringComparer.Ordinal);
        private readonly List<string>? _operations;

        public FakeSecurityBanLookupCache(List<string>? operations = null)
        {
            _operations = operations;
        }

        public int SetCalls { get; private set; }

        public int SetMissCalls { get; private set; }

        public int RemoveCalls { get; private set; }

        public string LastRemovedKey { get; private set; } = string.Empty;

        public ValueTask<SecurityBanLookupCacheResult?> GetAsync(
            string banType,
            string target,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_values.TryGetValue(Key(banType, target), out var value) ? value : null);
        }

        public ValueTask SetAsync(
            SecurityBanRecord record,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            SetCalls++;
            _values[Key(record.BanType, record.Target)] = new SecurityBanLookupCacheResult(record);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetMissAsync(
            string banType,
            string target,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            SetMissCalls++;
            _values[Key(banType, target)] = SecurityBanLookupCacheResult.Miss;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(
            string banType,
            string target,
            CancellationToken cancellationToken)
        {
            RemoveCalls++;
            LastRemovedKey = Key(banType, target);
            _values.Remove(LastRemovedKey);
            _operations?.Add("cache-remove");
            return ValueTask.CompletedTask;
        }

        private static string Key(string banType, string target) => $"security:active-ban:{banType}:{target}";
    }

    private sealed class FakeSecurityAlertService : ISecurityAlertService
    {
        public int Count { get; private set; }

        public Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
        {
            Count++;
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
}
