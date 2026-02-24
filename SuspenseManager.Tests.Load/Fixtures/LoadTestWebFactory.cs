using Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Load.Fixtures;

/// <summary>
/// Фабрика тестового сервера для нагрузочных тестов.
/// Использует InMemory базу данных, предварительно заполненную тестовыми данными.
/// </summary>
public class LoadTestWebFactory : WebApplicationFactory<Program>, IAsyncDisposable
{

    private readonly string _dbName = $"LoadTest_{Guid.NewGuid():N}";

    public string? AccessToken { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<SuspenseManagerDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<SuspenseManagerDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>
    /// Засевает тестовые данные: 500 суспенсов без продукта, 300 с продуктом без прав.
    /// Создаёт тестовый аккаунт и получает JWT.
    /// </summary>
    public async Task SeedDataAndAuthenticateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SuspenseManagerDbContext>();

        // Создаём тестовый аккаунт
        var account = new Account
        {
            Login = "loadtestuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("LoadTest123!"),
            ArchiveLevel = 0,
            CreateTime = DateTime.UtcNow
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        // Создаём тип продукта
        var productType = new CatalogProductType
        {
            Code = "DIGI",
            Description = "Digital",
            CreateTime = DateTime.UtcNow
        };
        db.CatalogProductTypes.Add(productType);
        await db.SaveChangesAsync();

        // Создаём продукты для суспенсов статуса 1
        var products = new List<CatalogProduct>();
        for (var i = 0; i < 30; i++)
        {
            var p = new CatalogProduct
            {
                Isrc = $"ISRC{i:D6}",
                Barcode = $"{i:D13}",
                CatalogNumber = $"CAT-{i:D4}",
                ProductFormatCode = "DIGI",
                ProductName = $"Album {i}",
                Artist = $"Artist {i % 10}",
                ProductTypeId = productType.Id,
                CreateTime = DateTime.UtcNow,
                ArchiveLevel = 0
            };
            products.Add(p);
        }
        db.CatalogProducts.AddRange(products);
        await db.SaveChangesAsync();

        // 500 суспенсов без продукта (статус 0)
        var noProductLines = Enumerable.Range(0, 500).Select(i => new SuspenseLine
        {
            BusinessStatus = (int)BusinessStatus.NoProduct,
            Artist = $"Artist {i % 20}",
            TerritoryCode = i % 3 == 0 ? "RU" : i % 3 == 1 ? "US" : "GB",
            Isrc = $"NO_PROD_{i:D6}",
            AgreementNumber = $"AGR-{i:D4}",
            SenderCompany = $"Sender {i % 5}",
            RecipientCompany = $"Recipient {i % 5}",
            Qty = 100 + i,
            ExchangeCurrency = 1m,
            ExchangeRate = 1m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
            GroupId = null
        }).ToList();
        db.SuspenseLines.AddRange(noProductLines);

        // 300 суспенсов без прав (статус 1)
        var noRightsLines = Enumerable.Range(0, 300).Select(i => new SuspenseLine
        {
            BusinessStatus = (int)BusinessStatus.NoRights,
            ProductId = products[i % products.Count].Id,
            Artist = $"Artist {i % 10}",
            TerritoryCode = i % 2 == 0 ? "RU" : "US",
            Isrc = products[i % products.Count].Isrc,
            AgreementNumber = $"AGR-{i:D4}",
            SenderCompany = $"Sender {i % 5}",
            RecipientCompany = $"Recipient {i % 5}",
            Qty = 200 + i,
            ExchangeCurrency = 1m,
            ExchangeRate = 90m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
            GroupId = null
        }).ToList();
        db.SuspenseLines.AddRange(noRightsLines);

        await db.SaveChangesAsync();

        // Получаем JWT
        var httpClient = CreateClient();
        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            login = "loadtestuser",
            password = "LoadTest123!"
        });

        var body = await loginResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        AccessToken = body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
