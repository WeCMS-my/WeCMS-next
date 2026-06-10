using WeCms.Infrastructure.Time;
using WeCms.Shared.Time;

namespace WeCms.Tests.Unit.Infrastructure.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnUtcTime()
    {
        IClock clock = new SystemClock();
        var now = clock.UtcNow;

        // Should be UTC (Offset = 0)
        Assert.Equal(TimeSpan.Zero, now.Offset);
        // Should be within 1 second of actual UTC time
        Assert.True((DateTimeOffset.UtcNow - now).Duration() < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UtcNow_ShouldReturnCurrentTime()
    {
        IClock clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;
        var result = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        Assert.True(result >= before);
        Assert.True(result <= after);
    }
}
