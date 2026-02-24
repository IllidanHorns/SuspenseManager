using System.Net.Http.Headers;
using FluentAssertions;
using NBomber.CSharp;
using SuspenseManager.Tests.Load.Fixtures;

namespace SuspenseManager.Tests.Load;

/// <summary>
/// Нагрузочные тесты SuspenseManager API.
///
/// Используют NBomber для имитации параллельных пользователей.
/// Тесты запускаются против InMemory-сервера (WebApplicationFactory),
/// поэтому измеряют задержку приложения без задержки сети и базы данных.
///
/// Цели производительности (SLA):
///   - GET /api/group/no-product:  P99 &lt; 500 мс,  успех &gt; 99%
///   - POST /api/auth/login:       P99 &lt; 3000 мс, успех &gt; 95%
///   - Смешанная нагрузка:         успех &gt; 95%
/// </summary>
public class LoadTests : IAsyncLifetime
{
    private LoadTestWebFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new LoadTestWebFactory();
        await _factory.SeedDataAndAuthenticateAsync();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.AccessToken);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await ((IAsyncDisposable)_factory).DisposeAsync();
    }

    // ─────────────────── GET /api/group/no-product ────────────────────────────

    /// <summary>
    /// 50 параллельных пользователей, 10 секунд.
    /// Ожидаем: P99 &lt; 500 мс, успех &gt; 99%.
    /// </summary>
    [Fact]
    public void Load_GetNoProductGroups_50Concurrent_10Sec()
    {
        var scenario = Scenario.Create("get_no_product_groups", async context =>
        {
            var response = await _client.GetAsync("/api/group/no-product");

            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail(message: $"HTTP {(int)response.StatusCode}");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(2))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        var p99 = scenarioStats.Ok.Latency.Percent99;
        var okRate = scenarioStats.Ok.Request.Percent;

        p99.Should().BeLessThan(500,
            $"P99 для GET /api/group/no-product должна быть < 500 мс, фактически: {p99} мс");
        okRate.Should().BeGreaterThan(99,
            $"Процент успешных запросов должен быть > 99%, фактически: {okRate}%");
    }

    // ─────────────────── Некорректные запросы к Preview ─────────────────────

    /// <summary>
    /// 30 параллельных пользователей, 10 секунд.
    /// Высокий поток некорректных запросов не должен ронять сервер.
    /// </summary>
    [Fact]
    public void Load_GroupingPreview_InvalidStatus_Returns400_HighThroughput()
    {
        var scenario = Scenario.Create("grouping_preview_bad_requests", async context =>
        {
            var response = await _client.GetAsync(
                "/api/grouping/preview?BusinessStatus=99&GroupByColumns=Isrc&PageNumber=1&PageSize=10");

            // Ожидаем 400 (INVALID_STATUS), не 500
            return response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? Response.Ok()
                : Response.Fail(message: $"Expected 400, got {(int)response.StatusCode}");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(1))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var okRate = stats.ScenarioStats[0].Ok.Request.Percent;

        okRate.Should().BeGreaterThan(95,
            "Некорректные запросы должны давать 400, а не 500");
    }

    // ─────────────────── POST /api/auth/login ─────────────────────────────────

    /// <summary>
    /// 20 параллельных пользователей, 10 секунд логинятся.
    /// BCrypt — медленная операция, P99 допускаем до 3 сек.
    /// </summary>
    [Fact]
    public void Load_Login_20Concurrent_10Sec()
    {
        var scenario = Scenario.Create("auth_login", async context =>
        {
            using var tempClient = _factory.CreateClient();

            var response = await tempClient.PostAsJsonAsync("/api/auth/login", new
            {
                login = "loadtestuser",
                password = "LoadTest123!"
            });

            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail(message: $"HTTP {(int)response.StatusCode}");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(2))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats[0];
        var p99 = scenarioStats.Ok.Latency.Percent99;
        var okRate = scenarioStats.Ok.Request.Percent;

        p99.Should().BeLessThan(3000,
            $"P99 логина должна быть < 3 сек, фактически: {p99} мс");
        okRate.Should().BeGreaterThan(95,
            $"Успех логинов > 95%, фактически: {okRate}%");
    }

    // ─────────────────── GET /api/group/{id} несуществующая ────────────────────

    [Fact]
    public void Load_GetGroupById_NotFound_HighVolume()
    {
        var scenario = Scenario.Create("get_group_not_found", async context =>
        {
            var groupId = Random.Shared.Next(900_000, 999_999);
            var response = await _client.GetAsync($"/api/group/{groupId}");

            return response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? Response.Ok()
                : Response.Fail(message: $"Expected 404, got {(int)response.StatusCode}");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(1))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 40, during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var p99 = stats.ScenarioStats[0].Ok.Latency.Percent99;
        var okRate = stats.ScenarioStats[0].Ok.Request.Percent;

        p99.Should().BeLessThan(200, "Поиск несуществующей группы — P99 < 200 мс");
        okRate.Should().BeGreaterThan(99);
    }

    // ─────────────────── Смешанная нагрузка ──────────────────────────────────

    /// <summary>
    /// Несколько сценариев одновременно имитируют реальный трафик.
    /// </summary>
    [Fact]
    public void Load_MixedWorkload_Stable()
    {
        var readGroupsScenario = Scenario.Create("read_groups", async context =>
        {
            var response = await _client.GetAsync("/api/group/no-product?PageNumber=1&PageSize=20");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail(message: "read_groups failed");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(1))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(15))
        );

        var readNoRightsScenario = Scenario.Create("read_no_rights", async context =>
        {
            var response = await _client.GetAsync("/api/group/no-rights?PageNumber=1&PageSize=20");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail(message: "read_no_rights failed");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(1))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(15))
        );

        var notFoundScenario = Scenario.Create("not_found_requests", async context =>
        {
            var response = await _client.GetAsync("/api/group/999999");
            return response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? Response.Ok()
                : Response.Fail(message: "Expected 404");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(1))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 15, during: TimeSpan.FromSeconds(15))
        );

        var stats = NBomberRunner
            .RegisterScenarios(readGroupsScenario, readNoRightsScenario, notFoundScenario)
            .WithoutReports()
            .Run();

        foreach (var scenario in stats.ScenarioStats)
        {
            scenario.Ok.Request.Percent.Should().BeGreaterThan(95,
                $"Сценарий '{scenario.ScenarioName}' должен иметь > 95% успешных запросов");
        }
    }

    // ─────────────────── Ramp-Up нагрузка ────────────────────────────────────

    /// <summary>
    /// Постепенное наращивание нагрузки: 5 → 50 пользователей.
    /// </summary>
    [Fact]
    public void Load_RampUp_5To50Users()
    {
        var scenario = Scenario.Create("ramp_up", async context =>
        {
            var response = await _client.GetAsync("/api/group/no-product?PageNumber=1&PageSize=10");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail(message: "ramp_up failed");
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(2))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(5)),
            Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(5)),
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(copies: 10, during: TimeSpan.FromSeconds(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var okRate = stats.ScenarioStats[0].Ok.Request.Percent;

        okRate.Should().BeGreaterThan(95,
            "При ramp-up 5→50 пользователей успех должен быть > 95%");
    }
}
