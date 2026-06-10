namespace WeCms.Shared;

public sealed class DomainException : Exception
{
    public int Code { get; }

    public DomainException(int code, string message) : base(message)
    {
        Code = code;
    }

    public DomainException(int code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
