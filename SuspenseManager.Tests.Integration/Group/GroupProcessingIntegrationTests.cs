using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Models.Enums;
using SuspenseManager.Tests.Integration.Fixtures;

namespace SuspenseManager.Tests.Integration.Group;

/// <summary>
/// Интеграционные тесты GroupProcessingController.
/// Проверяют HTTP-уровень операций над группами: postpone, ungroup, back-office, link-product.
/// </summary>
public class GroupProcessingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GroupProcessingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> AuthorizeAsync(string loginSuffix = "proc")
    {
        var login = $"procuser_{loginSuffix}_{Guid.NewGuid():N}";
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync(login, "Pass123!");
        });

        var resp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            login,
            password = "Pass123!"
        });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ─────────────────────────── GET /api/group/no-product ───────────────────

    [Fact]
    public async Task GetNoProduct_Returns200()
    {
        var token = await AuthorizeAsync("getnoprod");
        SetBearer(token);

        var response = await _client.GetAsync("/api/group/no-product");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNoProduct_ContainsOnlyStatus15Groups()
    {
        var token = await AuthorizeAsync("getnoprod2");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            groupId = group.Id;
        });

        var response = await _client.GetAsync("/api/group/no-product");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var items = body.GetProperty("data").GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        // Все группы должны иметь статус 15
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("businessStatus").GetInt32().Should().Be((int)BusinessStatus.InGroupNoProduct);
        }
    }

    // ─────────────────────────── GET /api/group/no-rights ────────────────────

    [Fact]
    public async Task GetNoRights_Returns200()
    {
        var token = await AuthorizeAsync("getnorights");
        SetBearer(token);

        var response = await _client.GetAsync("/api/group/no-rights");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────── GET /api/group/{id} ─────────────────────────

    [Fact]
    public async Task GetById_ExistingGroup_Returns200()
    {
        var token = await AuthorizeAsync("getbyid");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            groupId = group.Id;
        });

        var response = await _client.GetAsync($"/api/group/{groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistentGroup_Returns404()
    {
        var token = await AuthorizeAsync("getbyid404");
        SetBearer(token);

        var response = await _client.GetAsync("/api/group/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────── POST /api/groups/{id}/postpone ──────────────────

    [Fact]
    public async Task Postpone_Status15Group_Returns200_And_NewStatus30()
    {
        var token = await AuthorizeAsync("postpone");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoProduct, groupId: group.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/postpone", new { reason = "Test postpone" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Проверяем что статус группы обновился
        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.BusinessStatus.Should().Be((int)BusinessStatus.PostponedNoProduct);
        });
    }

    [Fact]
    public async Task Postpone_Status16Group_Returns200_And_NewStatus32()
    {
        var token = await AuthorizeAsync("postpone16");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoRights, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoRights, groupId: group.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/postpone", new { reason = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.BusinessStatus.Should().Be((int)BusinessStatus.PostponedNoRights);
        });
    }

    [Fact]
    public async Task Postpone_NonExistentGroup_Returns404()
    {
        var token = await AuthorizeAsync("postpone404");
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            "/api/groups/999999/postpone", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────── POST /api/groups/{id}/send-to-backoffice ────────────

    [Fact]
    public async Task SendToBackOffice_Status15_Returns200_And_Status120()
    {
        var token = await AuthorizeAsync("backoffice");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoProduct, groupId: group.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/send-to-backoffice",
            new { comment = "Complex case" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.BusinessStatus.Should().Be((int)BusinessStatus.BackOfficeNoProduct);
        });
    }

    // ─────────────────────── POST /api/groups/{id}/ungroup ───────────────────

    [Fact]
    public async Task Ungroup_Status15_Returns200_And_GroupArchived()
    {
        var token = await AuthorizeAsync("ungroup");
        SetBearer(token);

        int groupId = 0;
        int suspenseId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            var suspense = await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoProduct, groupId: group.Id);
            groupId = group.Id;
            suspenseId = suspense.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/ungroup", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.ArchiveLevel.Should().BeGreaterThan(0, "группа должна быть архивирована");

            var suspense = await db.SuspenseLines.FindAsync(suspenseId);
            suspense!.GroupId.Should().BeNull();
            suspense.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
        });
    }

    [Fact]
    public async Task Ungroup_Status16_PreservesProductIdOnSuspenses()
    {
        // Бизнес-правило 5: при разгруппировке статус 1 → ProductId сохраняется
        var token = await AuthorizeAsync("ungroup16");
        SetBearer(token);

        int groupId = 0;
        int suspenseId = 0;
        int productId = 0;

        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var product = await builder.CreateProductAsync();
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoRights, account.Id, product.Id);
            var suspense = await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoRights,
                groupId: group.Id,
                productId: product.Id);
            groupId = group.Id;
            suspenseId = suspense.Id;
            productId = product.Id;
        });

        await _client.PostAsJsonAsync($"/api/groups/{groupId}/ungroup", new { });

        await _factory.WithDbAsync(async db =>
        {
            var suspense = await db.SuspenseLines.FindAsync(suspenseId);
            suspense!.ProductId.Should().Be(productId,
                "ProductId должен сохраниться при разгруппировке 16 — бизнес-правило 5");
            suspense.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
        });
    }

    // ─────────────────── POST /api/groups/{id}/link-product ──────────────────

    [Fact]
    public async Task LinkProduct_Status15_Returns200_And_GroupBecomesStatus16()
    {
        var token = await AuthorizeAsync("linkprod");
        SetBearer(token);

        int groupId = 0;
        int productId = 0;

        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var product = await builder.CreateProductAsync();
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoProduct, groupId: group.Id);
            groupId = group.Id;
            productId = product.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/link-product",
            new { productId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
            group.CatalogProductId.Should().Be(productId);
        });
    }

    [Fact]
    public async Task LinkProduct_NonExistentProduct_Returns404()
    {
        var token = await AuthorizeAsync("linkprod404");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/link-product",
            new { productId = 999999 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────── POST /api/groups/{id}/catalog-fast ──────────────────

    [Fact]
    public async Task QuickCatalog_Status15_Returns200_And_NewProduct()
    {
        var token = await AuthorizeAsync("quickcat");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.InGroupNoProduct, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.InGroupNoProduct,
                artist: "New Artist",
                groupId: group.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/groups/{groupId}/catalog-fast", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    // ─────────────── POST /api/postponed/{id}/return ─────────────────────────

    [Fact]
    public async Task ReturnFromPostponed_Status30_Returns200_And_Status15()
    {
        var token = await AuthorizeAsync("return");
        SetBearer(token);

        int groupId = 0;
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            var account = await builder.CreateAccountAsync($"acc_{Guid.NewGuid():N}", "P123!");
            var group = await builder.CreateGroupAsync(
                (int)BusinessStatus.PostponedNoProduct, account.Id);
            await builder.CreateSuspenseLineAsync(
                (int)BusinessStatus.PostponedNoProduct, groupId: group.Id);
            groupId = group.Id;
        });

        var response = await _client.PostAsJsonAsync(
            $"/api/postponed/{groupId}/return", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var group = await db.SuspenseGroups.FindAsync(groupId);
            group!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoProduct);
        });
    }

    // ─────────────────── Без авторизации — 401 ───────────────────────────────

    [Fact]
    public async Task GroupEndpoints_WithoutAuth_Return401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var endpoints = new[]
        {
            ("/api/group/no-product", HttpMethod.Get),
            ("/api/group/no-rights",  HttpMethod.Get),
            ("/api/group/1",          HttpMethod.Get),
        };

        foreach (var (path, method) in endpoints)
        {
            var request = new HttpRequestMessage(method, path);
            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"эндпоинт {path} должен требовать авторизацию");
        }
    }
}
