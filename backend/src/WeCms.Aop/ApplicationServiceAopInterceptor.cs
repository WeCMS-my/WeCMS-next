using Castle.DynamicProxy;

namespace WeCms.Aop;

public sealed class ApplicationServiceAopInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        invocation.Proceed();
    }
}
