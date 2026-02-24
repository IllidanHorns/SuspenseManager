using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common.DTOs;
using FluentAssertions;
using SuspenseManager.Tests.Integration.Fixtures;

namespace SuspenseManager.Tests.Integration.Auth;

/// <summary>
/// Интеграционные тесты AuthController.
/// Проверяют полный HTTP-стек: логин, refresh, revoke.
/// </summary>
public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<TestDataBuilder> GetBuilderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.SuspenseManagerDbContext>();
        return new TestDataBuilder(db);
    }

    // ──────────────────────── POST /api/auth/login ────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("loginuser1", "Pass123!");
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "loginuser1",
            password = "Pass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("loginuser2", "CorrectPass!");
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "loginuser2",
            password = "WrongPass!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "ghost_user_xyz",
            password = "AnyPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns_BusinessCode_INVALID_CREDENTIALS_On_WrongPassword()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("loginuser3", "CorrectPass!");
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "loginuser3",
            password = "WrongPass!"
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("businessCode").GetString().Should().Be("INVALID_CREDENTIALS");
    }

    // ──────────────────────── POST /api/auth/refresh ──────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("refreshuser1", "Pass123!");
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "refreshuser1",
            password = "Pass123!"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("data").GetProperty("refreshToken").GetString()!;

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("data").GetProperty("refreshToken").GetString().Should().NotBe(refreshToken,
            "токен должен быть ротирован");
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "completely_invalid_token_xyz"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RevokedToken_Returns401()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("refreshuser2", "Pass123!");
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "refreshuser2",
            password = "Pass123!"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("data").GetProperty("refreshToken").GetString()!;

        // Отзываем токен
        _client.DefaultRequestHeaders.Clear();
        var accessToken = loginBody.GetProperty("data").GetProperty("accessToken").GetString()!;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        await _client.PostAsJsonAsync("/api/auth/revoke", new { refreshToken });
        _client.DefaultRequestHeaders.Authorization = null;

        // Пытаемся использовать отозванный токен
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────── POST /api/auth/revoke ───────────────────────────

    [Fact]
    public async Task Revoke_ValidToken_Returns200()
    {
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("revokeuser1", "Pass123!");
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "revokeuser1",
            password = "Pass123!"
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("data").GetProperty("refreshToken").GetString()!;
        var accessToken = loginBody.GetProperty("data").GetProperty("accessToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_RequiresAuthentication_Returns401WithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = "any_token_value"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
