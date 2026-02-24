using Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Integration.Fixtures;

/// <summary>
/// Фабрика тестового веб-приложения.
/// Заменяет SQL Server на InMemory-базу данных, отключает seed тестовых данных.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Уникальный идентификатор для изоляции тестовых баз данных между экземплярами фабрики.
    /// </summary>
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Удаляем реальный DbContext (SQL Server)
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<SuspenseManagerDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Регистрируем InMemory DbContext
            services.AddDbContext<SuspenseManagerDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });
    }

    /// <summary>
    /// Получает область видимости с настроенным DbContext для настройки данных в тестах.
    /// </summary>
    public async Task<T> WithDbAsync<T>(Func<SuspenseManagerDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SuspenseManagerDbContext>();
        return await action(db);
    }

    /// <summary>
    /// Выполняет действие в рамках контекста базы данных.
    /// </summary>
    public async Task WithDbAsync(Func<SuspenseManagerDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SuspenseManagerDbContext>();
        await action(db);
    }
}
