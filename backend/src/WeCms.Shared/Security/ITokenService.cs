namespace WeCms.Shared.Security;

public interface ITokenService
{
    string GenerateAccessToken(CurrentUser user);
}
