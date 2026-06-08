 namespace WeCms.Shared;
 
 public static class ApiCodes
 {
     public const int Success = 0;
     public const int Unauthorized = 401;
     public const int Forbidden = 403;
     public const int NotFound = 404;
     public const int Conflict = 409;
     public const int TooManyRequests = 429;
     public const int ValidationError = 1001;
     public const int BusinessError = 2001;
     public const int SystemError = 5000;
 }
