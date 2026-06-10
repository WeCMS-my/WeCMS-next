namespace WeCms.Shared.Security;

public interface ITokenGenerator
{
    string GenerateRefreshToken();
}
