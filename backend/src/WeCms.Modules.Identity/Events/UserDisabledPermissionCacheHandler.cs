using WeCms.EventBus;
using WeCms.Modules.Identity.Services;

namespace WeCms.Modules.Identity.Events;

public sealed class UserDisabledPermissionCacheHandler(IIdentityPermissionVersionService permissionVersionService)
    : IEventHandler<UserDisabledEvent>
{
    public Task HandleAsync(UserDisabledEvent integrationEvent, CancellationToken cancellationToken)
    {
        return permissionVersionService.BumpUserAsync(integrationEvent.UserId, integrationEvent.OccurredAt, cancellationToken);
    }
}
