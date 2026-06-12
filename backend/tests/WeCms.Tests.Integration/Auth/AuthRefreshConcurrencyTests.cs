using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WeCms.Shared;
using WeCms.Modules.System.Auth;

namespace WeCms.Tests.Integration.Auth;

public sealed class AuthRefreshConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthRefreshConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_WithSameTokenConcurrently_ShouldAllowOnlyOneSuccess()
    {
        var client = _client;

        var loginResponse = await LoginAsync("admin", "Admin@123");
        Assert.NotNull(loginResponse.Data);
        Assert.Equal(ApiCodes.Success, loginResponse.Code);
        Assert.False(loginResponse.Data!.RequiresTwoFactor);
        Assert.NotNull(loginResponse.Data.RefreshToken);

        var refreshRequest = new RefreshRequest(loginResponse.Data.RefreshToken);
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
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadApiResultAsync<LoginResponse>(response);
    }
}
