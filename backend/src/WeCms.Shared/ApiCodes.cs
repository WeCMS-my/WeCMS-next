namespace WeCms.Shared;

public static class ApiCodes
{
    public const int Success = 0;
    public const int ValidationError = 40001;
    public const int Unauthorized = 40101;
    public const int Forbidden = 40301;
    public const int NotFound = 40401;
    public const int Conflict = 40901;
    public const int TooManyRequests = 42901;
    public const int BusinessError = 50001;
    public const int SystemError = 50000;
}
