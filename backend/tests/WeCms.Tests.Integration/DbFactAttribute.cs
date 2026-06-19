using System.Runtime.CompilerServices;
using Xunit;

namespace WeCms.Tests.Integration;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DbFactAttribute : FactAttribute
{
    public DbFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!IntegrationTestDatabase.IsDatabaseAvailable(out var reason))
        {
            Skip = $"Integration test skipped: MySQL database is unavailable. {reason}";
        }
    }
}