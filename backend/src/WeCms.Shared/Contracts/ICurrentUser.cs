namespace WeCms.Shared.Contracts;

public interface ICurrentUser
{
    long UserId { get; }
    string Username { get; }
    string? IpAddress { get; }
    bool IsSuperAdmin { get; }
}
