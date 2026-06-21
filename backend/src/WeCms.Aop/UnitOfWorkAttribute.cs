namespace WeCms.Aop;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UnitOfWorkAttribute : Attribute
{
    public int Order { get; init; }
}
