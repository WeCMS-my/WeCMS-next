namespace WeCms.Shared.Security;

public interface IRefreshTokenHasher
{
    string Hash(string token);
}
