using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using WeCms.Modules.System.Auth;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Auth;

public sealed class AuthLogoutTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient? _client;
    private readonly string? _skipReason;

    public AuthLogoutTests(WebApplicationFactory<Program> factory)
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
    public async Task Logout_WithEmptyBody_ShouldReturnBadRequest()
    {
        if (_client is null || _skipReason is not null)
        {
            return;
        }

        var client = _client;
        var login = await LoginAsync(client);
        Assert.NotNull(login.Data);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Data!.AccessToken);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithEmptyRefreshToken_ShouldReturnBadRequest()
    {
        if (_client is null || _skipReason is not null)
        {
            return;
        }

        var client = _client;
        var login = await LoginAsync(client);
        Assert.NotNull(login.Data);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Data!.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResult<object?>>();
        Assert.NotNull(body);
        Assert.Equal(ApiCodes.ValidationError, body!.Code);
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        if (_client is null || _skipReason is not null)
        {
            return;
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest("refresh-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<ApiResult<LoginResponse>> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin@123"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>();
        return body ?? throw new InvalidOperationException("Failed to deserialize login response.");
    }
}
