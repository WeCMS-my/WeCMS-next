namespace WeCms.Shared;

public sealed class DomainException : Exception
{
    public DomainException(int code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public int Code { get; }

    public int StatusCode { get; }
}
