using WeCms.Modules.AccessControl.Records;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed class EndpointPermissionDeniedRecorder : IEndpointPermissionDeniedRecorder
{
    private readonly IPermissionSecurityEventWriter _securityEventWriter;
    private readonly IAccessControlClock _clock;

    public EndpointPermissionDeniedRecorder(
        IPermissionSecurityEventWriter securityEventWriter,
        IAccessControlClock clock)
    {
        _securityEventWriter = securityEventWriter;
        _clock = clock;
    }

    public Task RecordAsync(
        long userId,
        string? username,
        string permissionCode,
        string ip,
        string reason,
        string traceId,
        CancellationToken cancellationToken)
    {
        return _securityEventWriter.RecordAsync(
            new PermissionSecurityEventRecord(
                "permission_denied",
                userId,
                username,
                ip,
                $"{reason} Required permission: {permissionCode}.",
                _clock.UtcNow,
                traceId),
            cancellationToken);
    }
}
