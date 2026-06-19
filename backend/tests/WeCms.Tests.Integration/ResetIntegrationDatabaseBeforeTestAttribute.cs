using System.Reflection;
using Xunit.v3;

namespace WeCms.Tests.Integration;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResetIntegrationDatabaseBeforeTestAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        IntegrationTestDatabase.ResetDatabaseAsync(IntegrationTestDatabase.GetConnectionString()).GetAwaiter().GetResult();
    }
}
