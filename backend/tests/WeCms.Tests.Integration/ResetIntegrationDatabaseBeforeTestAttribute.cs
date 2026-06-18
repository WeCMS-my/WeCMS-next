using System.Reflection;
using Xunit.Sdk;

namespace WeCms.Tests.Integration;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResetIntegrationDatabaseBeforeTestAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        IntegrationTestDatabase.ResetDatabaseAsync(IntegrationTestDatabase.GetConnectionString()).GetAwaiter().GetResult();
    }
}
