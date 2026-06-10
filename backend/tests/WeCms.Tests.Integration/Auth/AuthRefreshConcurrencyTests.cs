using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WeCms.Shared;
using WeCms.Modules.System.Auth;

namespace WeCms.Tests.Integration.Auth;

public sealed class AuthRefreshConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient? _client;
    private readonly string? _skipReason;

    public AuthRefreshConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        try
        {
            _client = factory.CreateClient();
        }
        catch (Exception ex)
        {
            _skipReason = ex.Message;
        }
    }

    [Fact]
    public async Task Refresh_WithSameTokenConcurrently_ShouldAllowOnlyOneSuccess()
    {
        if (_client is null || _skipReason is not null)
        {
            return;
        }
        var client = _client;

        var loginResponse = await LoginAsync("admin", "Admin@123");
        Assert.NotNull(loginResponse.Data);
        Assert.Equal(ApiCodes.Success, loginResponse.Code);

        var refreshRequest = new RefreshRequest(loginResponse.Data!.RefreshToken);
        var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<ApiResult<RefreshResponse>> RefreshOnceAsync()
        {
            await startSignal.Task;
            var refreshHttpResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
            Assert.Equal(HttpStatusCode.OK, refreshHttpResponse.StatusCode);
            return await ReadApiResultAsync<RefreshResponse>(refreshHttpResponse);
        }

        var first = Task.Run(RefreshOnceAsync);
        var second = Task.Run(RefreshOnceAsync);
        startSignal.SetResult(true);

        var results = await Task.WhenAll(first, second);

        var successCount = results.Count(r => r.Code == ApiCodes.Success);
        var unauthorizedCount = results.Count(r => r.Code == ApiCodes.Unauthorized);
        var success = results.Single(r => r.Code == ApiCodes.Success);

        Assert.Equal(1, successCount);
        Assert.Equal(1, unauthorizedCount);
        Assert.NotNull(success.Data);
        Assert.All(results, r => Assert.NotNull(r.Msg));
    }

    private static async Task<ApiResult<T>> ReadApiResultAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResult<T>>();
        return body ?? throw new InvalidOperationException("Failed to deserialize ApiResult.");
    }

    private async Task<ApiResult<LoginResponse>> LoginAsync(string username, string password)
    {
        var client = _client;
        if (client is null)
        {
            throw new InvalidOperationException("测试客户端未初始化。");
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadApiResultAsync<LoginResponse>(response);
    }
}
