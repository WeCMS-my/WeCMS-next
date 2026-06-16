namespace WeCms.Shared;

public static class ApiCodes
{
    public const int Success = 0;
    public const int ValidationError = 40000;
    public const int BusinessError = 40001;
    public const int Unauthorized = 40100;
    public const int Forbidden = 40300;
    public const int NotFound = 40400;
    public const int Conflict = 40900;
    public const int TooManyRequests = 42900;
    public const int ServiceUnavailable = 50300;
    public const int SystemError = 50000;

    public static int ToHttpStatus(int code)
    {
        return code switch
        {
            Success => 200,
            ValidationError => 400,
            BusinessError => 400,
            Unauthorized => 401,
            Forbidden => 403,
            NotFound => 404,
            Conflict => 409,
            TooManyRequests => 429,
            ServiceUnavailable => 503,
            SystemError => 500,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown API code.")
        };
    }

    public static void ThrowIfUnknown(int code)
    {
        _ = ToHttpStatus(code);
    }
}
