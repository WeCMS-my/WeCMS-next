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
    private readonly HttpClient _client;

    public AuthLogoutTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_WithEmptyBody_ShouldReturnBadRequest()
    {
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
