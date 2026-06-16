namespace WeCms.Shared;

public sealed class DomainException : Exception
{
    public DomainException(
        int code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
        : base(message)
    {
        ApiCodes.ThrowIfUnknown(code);
        Code = code;
        FieldErrors = fieldErrors;
    }

    public int Code { get; }

    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; }
}
