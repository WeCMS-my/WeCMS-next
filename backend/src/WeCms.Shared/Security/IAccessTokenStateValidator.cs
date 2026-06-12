namespace WeCms.Shared.Security;

public interface IAccessTokenStateValidator
{
    Task<bool> ValidateAsync(AccessTokenState tokenState, CancellationToken cancellationToken);
}

public sealed record AccessTokenState(
    long UserId,
    int PermissionVersion,
    string SecurityStamp);
