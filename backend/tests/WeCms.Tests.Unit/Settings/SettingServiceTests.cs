using WeCms.Modules.System.Settings;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Settings;

public sealed class SettingServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new SettingService(new FakeSettingRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(new SettingListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetAsync_MasksSensitiveValue()
    {
        var service = new SettingService(new FakeSettingRepository { Sensitive = true, Value = "secret" });

        var detail = await service.GetAsync("site.secret", CancellationToken.None);

        Assert.True(detail.IsSensitive);
        Assert.Null(detail.Value);
    }

    [Fact]
    public async Task UpdateAsync_RecordsAuditForSensitiveSetting()
    {
        var repository = new FakeSettingRepository { Sensitive = true };
        var service = new SettingService(repository);

        await service.UpdateAsync("site.secret", new UpdateSettingRequest("changed"), Context(), CancellationToken.None);

        Assert.True(repository.AuditRecorded);
    }

    [Fact]
    public async Task UpdateAsync_RejectsMissingSetting()
    {
        var service = new SettingService(new FakeSettingRepository { Missing = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync("missing", new UpdateSettingRequest("value"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    private static SettingRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakeSettingRepository : ISettingRepository
    {
        public bool Sensitive { get; init; }
        public bool Missing { get; init; }
        public string Value { get; init; } = "current";
        public bool AuditRecorded { get; private set; }

        public Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListCriteria criteria, CancellationToken cancellationToken)
        {
            var dto = new SettingSummaryDto("site.name", Sensitive ? null : Value, "string", "site", "Site Name", null, Sensitive, true, DateTimeOffset.UnixEpoch, null);
            return Task.FromResult(new PagedResult<SettingSummaryDto>([dto], criteria.Page, criteria.PageSize, 1));
        }

        public Task<SettingDetailDto?> GetAsync(string key, CancellationToken cancellationToken)
        {
            if (Missing)
            {
                return Task.FromResult<SettingDetailDto?>(null);
            }

            return Task.FromResult<SettingDetailDto?>(new SettingDetailDto(key, Value, "string", "site", "Site Secret", null, Sensitive, true, DateTimeOffset.UnixEpoch, null));
        }

        public Task UpdateAsync(SettingUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RecordAuditAsync(SettingAuditRecord record, CancellationToken cancellationToken)
        {
            AuditRecorded = true;
            return Task.CompletedTask;
        }
    }
}
