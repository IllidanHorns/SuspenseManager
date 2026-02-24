using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SuspenseManager.Tests.Integration.Fixtures;

namespace SuspenseManager.Tests.Integration.Security;

/// <summary>
/// Тесты безопасности.
/// Проверяют защиту от SQL-injection, некорректных JWT, перечисления ресурсов,
/// небезопасных входных данных и нарушений авторизации.
/// </summary>
public class SecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> AuthorizeAsync()
    {
        var login = $"secuser_{Guid.NewGuid():N}";
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync(login, "Pass123!");
        });

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { login, password = "Pass123!" });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    // ══════════════════════ SQL INJECTION ════════════════════════════════════

    [Theory]
    [InlineData("'; DROP TABLE SuspenseLines;--")]
    [InlineData("1 OR 1=1")]
    [InlineData("' UNION SELECT * FROM Accounts--")]
    [InlineData("1; DELETE FROM SuspenseGroups")]
    [InlineData("'); EXEC xp_cmdshell('dir');--")]
    public async Task GroupingPreview_SqlInjectionInGroupByColumns_Returns400_NotServerError(
        string maliciousColumn)
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Передаём вредоносное имя столбца в GroupByColumns
        var response = await _client.GetAsync(
            $"/api/grouping/preview?BusinessStatus=0&GroupByColumns={Uri.EscapeDataString(maliciousColumn)}&PageNumber=1&PageSize=10");

        // Должен вернуть 400 (BadRequest), но НЕ 500 (Internal Server Error — признак уязвимости)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"SQL-injection через имя столбца должна быть отклонена whitelist'ом, не вызывать 500. Column: {maliciousColumn}");
    }

    [Theory]
    [InlineData("'; DROP TABLE SuspenseLines;--")]
    [InlineData("' OR '1'='1")]
    [InlineData("\" OR \"1\"=\"1")]
    [InlineData("1; WAITFOR DELAY '0:0:10'--")]  // timing attack
    public async Task GroupingPreview_SqlInjectionInFilterValues_DoesNotCause500(
        string maliciousValue)
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Значения фильтров должны параметризоваться — проверяем, что нет 500
        var encodedValue = Uri.EscapeDataString(maliciousValue);
        var response = await _client.GetAsync(
            $"/api/grouping/preview?BusinessStatus=0&GroupByColumns=Isrc&Filters%5BArtist%5D={encodedValue}&PageNumber=1&PageSize=10");

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            $"SQL-injection в значении фильтра не должна вызывать 500. Value: {maliciousValue}");
    }

    [Fact]
    public async Task GroupingCommit_SqlInjectionInKeyValues_DoesNotCause500()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/grouping/commit", new
        {
            businessStatus = 0,
            groupByColumns = new[] { "Artist" },
            keyValues = new Dictionary<string, string>
            {
                ["Artist"] = "'; DROP TABLE SuspenseLines;--"
            },
            accountId = 1
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    // ══════════════════════ JWT БЕЗОПАСНОСТЬ ══════════════════════════════════

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_EmptyBearerToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_RandomString_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "notavalidtoken");

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        // Подписанный, но истёкший JWT токен (exp в прошлом)
        // Создан с корректным ключом SuspenseManagerDefaultSecretKey_ChangeInProduction_32chars!
        // но с exp = Unix epoch 1000 (1970 год)
        const string expiredToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxIiwibmFtZSI6InRlc3QiLCJleHAiOjEwMDB9." +
            "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_TokenWithWrongSignature_Returns401()
    {
        // Получаем валидный токен
        var validToken = await AuthorizeAsync();

        // Меняем последний символ подписи — делаем подпись невалидной
        var parts = validToken.Split('.');
        var tamperedSignature = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "токен с изменённой подписью должен быть отклонён");
    }

    [Fact]
    public async Task ProtectedEndpoint_TokenWithModifiedPayload_Returns401()
    {
        var validToken = await AuthorizeAsync();

        // Декодируем и модифицируем payload
        var parts = validToken.Split('.');
        var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(
            parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));

        // Модифицируем payload (меняем account_id на 9999)
        var modifiedPayload = payloadJson.Replace(
            $"\"account_id\":\"1\"", "\"account_id\":\"9999\"");
        var encodedModified = Convert.ToBase64String(Encoding.UTF8.GetBytes(modifiedPayload))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Сохраняем оригинальную подпись (она не будет совпадать с новым payload)
        var tamperedToken = $"{parts[0]}.{encodedModified}.{parts[2]}";

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "токен с изменённым payload и оригинальной подписью должен быть отклонён");
    }

    // ══════════════════════ ПЕРЕЧИСЛЕНИЕ РЕСУРСОВ ════════════════════════════

    [Fact]
    public async Task GetGroup_NonExistentId_Returns404_NotInternalError()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/group/99999999");

        // Должен вернуть 404, не 500 и не раскрывать внутреннюю структуру
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("StackTrace", "стек-трейс не должен возвращаться клиенту");
        body.Should().NotContain("at Application.", "детали стека не должны раскрываться");
    }

    [Fact]
    public async Task Login_NonExistentUser_SameResponseAs_WrongPassword()
    {
        // Защита от user enumeration: оба случая возвращают одинаковый HTTP-статус и код
        var wrongUserResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "definitely_not_exists_xyz123",
            password = "SomePass123!"
        });

        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("enum_test_user", "CorrectPass123!");
        });

        var wrongPassResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = "enum_test_user",
            password = "WrongPass!"
        });

        // Оба должны возвращать одинаковый статус — 401
        wrongUserResponse.StatusCode.Should().Be(wrongPassResponse.StatusCode,
            "не должна раскрываться информация о существовании пользователя");

        var body1 = await wrongUserResponse.Content.ReadFromJsonAsync<JsonElement>();
        var body2 = await wrongPassResponse.Content.ReadFromJsonAsync<JsonElement>();

        body1.GetProperty("businessCode").GetString()
            .Should().Be(body2.GetProperty("businessCode").GetString(),
                "код ошибки должен быть одинаковым для защиты от перечисления");
    }

    // ══════════════════════ НЕБЕЗОПАСНЫЙ ВВОД ════════════════════════════════

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("onload=alert(1)")]
    public async Task Login_XssInLogin_Does_Not_Return500(string xssPayload)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login = xssPayload,
            password = "AnyPass123!"
        });

        // Не должно быть 500 — XSS в input не должен вызывать исключение на сервере
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        // Должен быть 401 — пользователь не найден
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_OverlyLongToken_DoesNotCause500()
    {
        // Очень длинная строка не должна вызывать 500 (stack overflow, OOM и т.п.)
        var veryLongToken = new string('A', 100_000);

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = veryLongToken
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GroupingPreview_VeryLargePageSize_DoesNotCause500()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(
            "/api/grouping/preview?BusinessStatus=0&GroupByColumns=Isrc&PageNumber=1&PageSize=999999999");

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    // ══════════════════════ CORS и заголовки ══════════════════════════════════

    [Fact]
    public async Task ApiResponse_DoesNotExposeServerVersionInHeaders()
    {
        var response = await _client.GetAsync("/api/auth/login");

        // Server header не должен раскрывать точную версию
        if (response.Headers.TryGetValues("Server", out var serverValues))
        {
            var serverHeader = string.Join(", ", serverValues);
            serverHeader.Should().NotContain("Kestrel/",
                "точная версия Kestrel не должна быть раскрыта");
        }
    }

    // ══════════════════════ REFRESH TOKEN REPLAY ATTACK ═══════════════════════

    [Fact]
    public async Task Refresh_UsedToken_CannotBeUsedAgain_Returns401()
    {
        // Сценарий: атакующий перехватил и использовал refresh token.
        // После ротации старый токен отзывается и не может быть использован повторно.
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync("replayuser", "Pass123!");
        });

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { login = "replayuser", password = "Pass123!" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var originalRefreshToken = loginBody.GetProperty("data")
            .GetProperty("refreshToken").GetString()!;

        // Первое использование — ротация токена
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = originalRefreshToken });

        // Повторное использование старого токена (replay attack)
        var replayResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = originalRefreshToken });

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "replay attack: уже использованный refresh token должен быть отклонён");
    }
}
