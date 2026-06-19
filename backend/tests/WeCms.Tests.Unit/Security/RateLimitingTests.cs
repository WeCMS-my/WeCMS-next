using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeCms.Api.RateLimiting;
using WeCms.Modules.System.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_NormalizesAndPersistsEvent()
    {
        var repository = new FakeRateLimitSecurityEventRepository();
        var alertService = new FakeSecurityAlertService();
        var service = new RateLimitSecurityEventService(repository, alertService);

        await service.RecordHitAsync(
            new RateLimitHitRecord(
                RateLimitPolicyNames.AuthLogin,
                "POST",
                "/api/v1/auth/login",
                42,
                "admin",
                " 192.168.1.10 ",
                "unit-test",
                "trace-rate",
                DateTimeOffset.Parse("2026-06-18T00:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture)),
            CancellationToken.None);

        Assert.NotNull(repository.Record);
        Assert.Equal("rate_limit_hit", repository.Record.EventType);
        Assert.Equal("warning", repository.Record.Severity);
        Assert.Equal("rate-limit", repository.Record.Source);
        Assert.Equal(RateLimitPolicyNames.AuthLogin, repository.Record.Policy);
        Assert.Equal("POST", repository.Record.HttpMethod);
        Assert.Equal("/api/v1/auth/login", repository.Record.Path);
        Assert.Equal("192.168.1.10", repository.Record.Ip);
        Assert.Equal("trace-rate", repository.Record.TraceId);
        Assert.Equal(1, alertService.Count);
    }

    [Fact]
    public async Task RateLimitSecurityEventService_RecordHitAsync_RejectsUnknownPolicy()
    {
        var service = new RateLimitSecurityEventService(new FakeRateLimitSecurityEventRepository(), new FakeSecurityAlertService());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordHitAsync(
                new RateLimitHitRecord("unknown_policy", "POST", "/api/v1/auth/login", null, null, "127.0.0.1", "unit-test", "trace-rate", DateTimeOffset.UtcNow),
                CancellationToken.None));
    }

    [Fact]
    public void RateLimitPolicyNames_DefinesRequiredH2Policies()
    {
        Assert.Contains("auth_login_policy", RateLimitPolicyNames.All);
        Assert.Contains("auth_refresh_policy", RateLimitPolicyNames.All);
        Assert.Contains("auth_2fa_policy", RateLimitPolicyNames.All);
        Assert.Contains("admin_write_policy", RateLimitPolicyNames.All);
        Assert.Contains("file_upload_policy", RateLimitPolicyNames.All);
        Assert.Contains("security_unban_policy", RateLimitPolicyNames.All);
    }

    [Fact]
    public void UserEndpointPartition_UsesUserIdAcrossPathsAndMethods()
    {
        var partitionById = GetUserEndpointPartition(CreateContext("/api/v1/admin/users", "POST", "10.0.0.1", 99));
        var partitionByIdOnOtherEndpoint = GetUserEndpointPartition(CreateContext("/api/v1/system/menus", "PUT", "10.0.0.1", 99));

        Assert.Equal(partitionById, partitionByIdOnOtherEndpoint);
    }

    [Fact]
    public void UserEndpointPartition_UsesIpForUnauthenticatedClientsAcrossPaths()
    {
        var partitionByIp = GetUserEndpointPartition(CreateContext("/api/v1/files/upload", "POST", "10.0.0.2"));
        var partitionByIpOnOtherEndpoint = GetUserEndpointPartition(CreateContext("/api/v1/security/bans/unban", "DELETE", "10.0.0.2"));

        Assert.Equal(partitionByIp, partitionByIpOnOtherEndpoint);
    }

    [Fact]
    public void UserEndpointPartition_DistinguishesDifferentUsers()
    {
        var firstUserPartition = GetUserEndpointPartition(CreateContext("/api/v1/users", "POST", "10.0.0.3", 7));
        var secondUserPartition = GetUserEndpointPartition(CreateContext("/api/v1/users", "PUT", "10.0.0.3", 8));

        Assert.NotEqual(firstUserPartition, secondUserPartition);
    }

    private static string GetUserEndpointPartition(DefaultHttpContext context)
    {
        var method = typeof(WeCmsRateLimitingExtensions).GetMethod(
            "UserEndpointPartition",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var partition = method.Invoke(null, new object[] { context });
        Assert.IsType<string>(partition);
        return partition as string ?? string.Empty;
    }

    private static DefaultHttpContext CreateContext(
        string path,
        string method,
        string remoteIp,
        long? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        if (userId is not null)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString(CultureInfo.InvariantCulture))],
                authenticationType: "test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private sealed class FakeRateLimitSecurityEventRepository : IRateLimitSecurityEventRepository
    {
        public RateLimitSecurityEventRecord? Record { get; private set; }

        public Task RecordHitAsync(RateLimitSecurityEventRecord record, CancellationToken cancellationToken)
        {
            Record = record;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityAlertService : ISecurityAlertService
    {
        public int Count { get; private set; }

        public Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }
}
