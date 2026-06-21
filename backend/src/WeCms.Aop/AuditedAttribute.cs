namespace WeCms.Aop;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuditedAttribute : Attribute
{
    public AuditedAttribute(string? operation = null)
    {
        Operation = operation;
    }

    public string? Operation { get; }

    public int Order { get; init; } = 300;
}
