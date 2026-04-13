using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

/// <summary>
/// Заполнение БД тестовыми данными для разработки
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Исправляет placeholder-хэши паролей для уже засидированных аккаунтов.
    /// Вызывать всегда (до основного Seed) — безопасно при любом состоянии БД.
    /// </summary>
    public static async Task EnsurePasswordsAsync(SuspenseManagerDbContext db, string adminPasswordHash)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Login == "admin" && a.PasswordHash == "placeholder_hash");

        if (account is not null)
        {
            account.PasswordHash = adminPasswordHash;
            account.ChangeTime = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedAsync(SuspenseManagerDbContext db, string adminPasswordHash)
    {
        // Справочник статусов — сидируем всегда (независимо от остальных данных)
        if (!await db.StatusDictionary.AnyAsync())
        {
            var statuses = new[]
            {
                new StatusDictionary { Code = 0,   Name = "Нет продукта",                    Description = "Суспенс не в группе, продукт не найден в каталоге" },
                new StatusDictionary { Code = 1,   Name = "Нет прав",                        Description = "Суспенс не в группе, продукт найден, права не определены" },
                new StatusDictionary { Code = 15,  Name = "В группе — нет продукта",         Description = "Суспенс в группе, ожидает каталогизации" },
                new StatusDictionary { Code = 16,  Name = "В группе — нет прав",             Description = "Суспенс в группе, продукт привязан, ожидает прав" },
                new StatusDictionary { Code = 30,  Name = "Отложено — нет продукта",         Description = "Группа без продукта отложена для позднейшей обработки" },
                new StatusDictionary { Code = 32,  Name = "Отложено — нет прав",             Description = "Группа без прав отложена для позднейшей обработки" },
                new StatusDictionary { Code = 88,  Name = "Валидировано",                    Description = "Суспенс прошёл валидацию, отправлен в расчёт роялти" },
                new StatusDictionary { Code = 120, Name = "Бэк-офис — нет продукта",         Description = "Группа без продукта передана в бэк-офис" },
                new StatusDictionary { Code = 320, Name = "Бэк-офис — нет прав",             Description = "Группа без прав передана в бэк-офис" },
            };
            db.StatusDictionary.AddRange(statuses);
            await db.SaveChangesAsync();
        }

        if (await db.Companies.AnyAsync())
        {
            return; // уже засидено
        }

        // === Территории ===
        var territories = new[]
        {
            new Territory { TerritoryCode = "RU", TerritoryName = "Россия", CreateTime = DateTime.UtcNow },
            new Territory { TerritoryCode = "US", TerritoryName = "США", CreateTime = DateTime.UtcNow },
            new Territory { TerritoryCode = "GB", TerritoryName = "Великобритания", CreateTime = DateTime.UtcNow },
            new Territory { TerritoryCode = "DE", TerritoryName = "Германия", CreateTime = DateTime.UtcNow },
            new Territory { TerritoryCode = "WW", TerritoryName = "Весь мир", CreateTime = DateTime.UtcNow },
        };
        db.Territories.AddRange(territories);
        await db.SaveChangesAsync();

        // === Компании ===
        var companies = new[]
        {
            new Company
            {
                LegalName = "ООО Мелодия Рекордс", ShortName = "Мелодия", CompanyCode = "MELODY",
                BankName = "Сбербанк", PhoneNumber = "+74951234567", Country = "RU",
                LegalAddress = "г. Москва, ул. Тверская, д. 1", ActualAddress = "г. Москва, ул. Тверская, д. 1",
                Inn = "7701234567", Bic = "044525225", CreateTime = DateTime.UtcNow
            },
            new Company
            {
                LegalName = "ООО Звук Дистрибьюция", ShortName = "Звук", CompanyCode = "ZVUK",
                BankName = "Тинькофф", PhoneNumber = "+74959876543", Country = "RU",
                LegalAddress = "г. Москва, ул. Ленина, д. 10", ActualAddress = "г. Москва, ул. Ленина, д. 10",
                Inn = "7709876543", Bic = "044525974", CreateTime = DateTime.UtcNow
            },
            new Company
            {
                LegalName = "Universal Music Group", ShortName = "UMG", CompanyCode = "UMG",
                BankName = "Deutsche Bank", PhoneNumber = "+12125551234", Country = "US",
                LegalAddress = "2220 Colorado Ave, Santa Monica, CA", ActualAddress = "2220 Colorado Ave, Santa Monica, CA",
                Inn = "US123456789", Bic = "DEUTDEFF", CreateTime = DateTime.UtcNow
            },
            new Company
            {
                LegalName = "Sony Music Entertainment", ShortName = "Sony Music", CompanyCode = "SONY",
                BankName = "JP Morgan", PhoneNumber = "+12125559999", Country = "US",
                LegalAddress = "25 Madison Ave, New York, NY", ActualAddress = "25 Madison Ave, New York, NY",
                Inn = "US987654321", Bic = "CHASUS33", CreateTime = DateTime.UtcNow
            },
            new Company
            {
                LegalName = "ООО Первое Музыкальное", ShortName = "ПервоеМуз", CompanyCode = "FIRST",
                BankName = "ВТБ", PhoneNumber = "+74951112233", Country = "RU",
                LegalAddress = "г. Санкт-Петербург, Невский пр., д. 5", ActualAddress = "г. Санкт-Петербург, Невский пр., д. 5",
                Inn = "7801112233", Bic = "044030702", CreateTime = DateTime.UtcNow
            },
        };
        db.Companies.AddRange(companies);
        await db.SaveChangesAsync();

        // === Типы продуктов ===
        var productTypes = new[]
        {
            new CatalogProductType { Code = "CD", Description = "Компакт-диск", CreateTime = DateTime.UtcNow },
            new CatalogProductType { Code = "VINYL", Description = "Виниловая пластинка", CreateTime = DateTime.UtcNow },
            new CatalogProductType { Code = "DIGI", Description = "Цифровой релиз", CreateTime = DateTime.UtcNow },
            new CatalogProductType { Code = "CASS", Description = "Кассета", CreateTime = DateTime.UtcNow },
        };
        db.CatalogProductTypes.AddRange(productTypes);
        await db.SaveChangesAsync();

        var digiType = productTypes[2]; // DIGI

        // === Продукты каталога ===
        var products = new[]
        {
            new CatalogProduct
            {
                Isrc = "RU1234567890", Barcode = "4607012345678", CatalogNumber = "CAT-001",
                ProductFormatCode = "DIGI", ProductTypeId = digiType.Id,
                ProductName = "Летний вечер", Artist = "Артём Иванов", AlbumName = "Лето 2025",
                Genre = "Pop", CreateTime = DateTime.UtcNow
            },
            new CatalogProduct
            {
                Isrc = "RU0987654321", Barcode = "4607098765432", CatalogNumber = "CAT-002",
                ProductFormatCode = "DIGI", ProductTypeId = digiType.Id,
                ProductName = "Ночной город", Artist = "Мария Петрова", AlbumName = "Огни",
                Genre = "Rock", CreateTime = DateTime.UtcNow
            },
            new CatalogProduct
            {
                Isrc = "US1234567890", Barcode = "0012345678905", CatalogNumber = "CAT-003",
                ProductFormatCode = "DIGI", ProductTypeId = digiType.Id,
                ProductName = "Midnight Dreams", Artist = "John Smith", AlbumName = "Night Sessions",
                Genre = "Electronic", CreateTime = DateTime.UtcNow
            },
            new CatalogProduct
            {
                Isrc = "GB9876543210", Barcode = "5012345678901", CatalogNumber = "CAT-004",
                ProductFormatCode = "CD", ProductTypeId = productTypes[0].Id,
                ProductName = "Rainy Day", Artist = "Emma Wilson", AlbumName = "Weather",
                Genre = "Jazz", CreateTime = DateTime.UtcNow
            },
            new CatalogProduct
            {
                Isrc = "RU5555555555", Barcode = "4607055555555", CatalogNumber = "CAT-005",
                ProductFormatCode = "DIGI", ProductTypeId = digiType.Id,
                ProductName = "Дорога домой", Artist = "Группа Путь", AlbumName = "Путешествие",
                Genre = "Rock", CreateTime = DateTime.UtcNow
            },
        };
        db.CatalogProducts.AddRange(products);
        await db.SaveChangesAsync();

        // === Права на продукты ===
        // Продукт 1: "Летний вечер" — есть полные права
        var rights = new[]
        {
            new CatalogProductRights
            {
                CatalogProductId = products[0].Id,
                CompanySender = "Мелодия", CompanySenderId = companies[0].Id,
                CompanyReceiver = "Звук", CompanyReceiverId = companies[1].Id,
                DocNumber = "DOC-2025-001", TerritoryCode = "RU", TerritoryDesc = "Россия",
                TerritoryId = territories[0].Id, Share = 50.0,
                DocStart = new DateOnly(2025, 1, 1), DocEnd = new DateOnly(2027, 12, 31),
                CreateTime = DateTime.UtcNow
            },
            // Продукт 2: "Ночной город" — есть права
            new CatalogProductRights
            {
                CatalogProductId = products[1].Id,
                CompanySender = "ПервоеМуз", CompanySenderId = companies[4].Id,
                CompanyReceiver = "Звук", CompanyReceiverId = companies[1].Id,
                DocNumber = "DOC-2025-002", TerritoryCode = "RU", TerritoryDesc = "Россия",
                TerritoryId = territories[0].Id, Share = 70.0,
                DocStart = new DateOnly(2025, 3, 1), DocEnd = new DateOnly(2026, 12, 31),
                CreateTime = DateTime.UtcNow
            },
            // Продукт 3: "Midnight Dreams" — есть права на US
            new CatalogProductRights
            {
                CatalogProductId = products[2].Id,
                CompanySender = "UMG", CompanySenderId = companies[2].Id,
                CompanyReceiver = "Sony Music", CompanyReceiverId = companies[3].Id,
                DocNumber = "DOC-2025-003", TerritoryCode = "US", TerritoryDesc = "США",
                TerritoryId = territories[1].Id, Share = 60.0,
                DocStart = new DateOnly(2025, 1, 1), DocEnd = new DateOnly(2028, 12, 31),
                CreateTime = DateTime.UtcNow
            },
            // Продукт 4: "Rainy Day" — НЕТ ПРАВ (чтобы тестировать статус 1)
            // Продукт 5: "Дорога домой" — НЕТ ПРАВ (чтобы тестировать статус 1)
        };
        db.CatalogProductRights.AddRange(rights);
        await db.SaveChangesAsync();

        // === Пользователь и аккаунт ===
        var user = new User
        {
            Name = "Админ",
            Surname = "Системный",
            MiddleName = "Тестович",
            Email = "admin@suspense.local",
            PhoneNumber = "+70001234567",
            Position = "Администратор",
            CreateTime = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var account = new Account
        {
            Login = "admin",
            PasswordHash = adminPasswordHash,
            Description = "Тестовый аккаунт администратора",
            UserId = user.Id,
            CreateTime = DateTime.UtcNow
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        // === Права доступа (permissions) ===
        var permissions = new[]
        {
            new Rights { Code = "uploads.view", Name = "Просмотр истории загрузок", Module = "Загрузка", CreateTime = DateTime.UtcNow },
            new Rights { Code = "uploads.create", Name = "Загрузка файлов", Module = "Загрузка", CreateTime = DateTime.UtcNow },
            new Rights { Code = "grouping.view", Name = "Просмотр группировки", Module = "Группировка", CreateTime = DateTime.UtcNow },
            new Rights { Code = "grouping.create", Name = "Создание групп", Module = "Группировка", CreateTime = DateTime.UtcNow },
            new Rights { Code = "groups.no_product.view", Name = "Просмотр групп (нет продукта)", Module = "Группы нет продукта", CreateTime = DateTime.UtcNow },
            new Rights { Code = "groups.no_product.catalog_fast", Name = "Быстрая каталогизация", Module = "Группы нет продукта", CreateTime = DateTime.UtcNow },
            new Rights { Code = "groups.no_product.possible_products", Name = "Поиск возможных продуктов", Module = "Группы нет продукта", CreateTime = DateTime.UtcNow },
            new Rights { Code = "groups.no_rights.view", Name = "Просмотр групп (нет прав)", Module = "Группы нет прав", CreateTime = DateTime.UtcNow },
            new Rights { Code = "groups.no_rights.correct_rights", Name = "Коррекция прав", Module = "Группы нет прав", CreateTime = DateTime.UtcNow },
            new Rights { Code = "admin.users.manage", Name = "Управление пользователями", Module = "Администрирование", CreateTime = DateTime.UtcNow },
            new Rights { Code = "admin.permissions.manage", Name = "Управление правами", Module = "Администрирование", CreateTime = DateTime.UtcNow },
        };
        db.Rights.AddRange(permissions);
        await db.SaveChangesAsync();

        // Назначаем все права админу
        foreach (var perm in permissions)
        {
            db.AccountRightsLinks.Add(new AccountRightsLink
            {
                AccountId = account.Id,
                RightId = perm.Id,
                CreateTime = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }
}
