namespace WeCms.Shared.Security;

/// <summary>
/// 密码哈希器抽象。
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

