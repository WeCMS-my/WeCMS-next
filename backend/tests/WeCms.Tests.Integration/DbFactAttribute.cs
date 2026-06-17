using Xunit;

namespace WeCms.Tests.Integration;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DbFactAttribute : FactAttribute
{
    public DbFactAttribute()
    {
        if (!IntegrationTestDatabase.IsDatabaseAvailable(out var reason))
        {
            Skip = $"Integration test skipped: MySQL database is unavailable. {reason}";
        }
    }
}
