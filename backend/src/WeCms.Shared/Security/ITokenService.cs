using WeCms.Shared.Security;

namespace WeCms.Shared.Security;

public interface ITokenService
{
    string GenerateAccessToken(CurrentUser user);
    TokenValidationResult ValidateAccessToken(string token);
}

public sealed record TokenValidationResult(bool IsValid, CurrentUser? User = null);
