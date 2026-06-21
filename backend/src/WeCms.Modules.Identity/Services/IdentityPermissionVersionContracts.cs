namespace WeCms.Modules.Identity.Services;

public interface IIdentityPermissionVersionService
{
    Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
}
