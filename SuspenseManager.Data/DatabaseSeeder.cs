using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

/// <summary>
/// Idempotent seeder — на каждом запуске добавляет только то, чего нет в БД.
/// </summary>
public static class DatabaseSeeder
{
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

    public static async Task SeedAsync(
        SuspenseManagerDbContext db,
        string adminPasswordHash,
        string operatorPasswordHash,
        string backOfficePasswordHash)
    {
        await SeedStatusDictionaryAsync(db);
        await SeedTerritoriesAsync(db);
        await SeedCompaniesAsync(db);
        await SeedProductTypesAsync(db);
        await SeedProductsAsync(db);
        await SeedProductRightsAsync(db);
        await SeedAdminAccountAsync(db, adminPasswordHash);
        await SeedPermissionsAsync(db);
        await SeedAdminPermissionLinksAsync(db);
        await SeedOperatorAccountAsync(db, operatorPasswordHash);
        await SeedBackOfficeAccountAsync(db, backOfficePasswordHash);
        await SeedDemoDataAsync(db);
    }

    // ── StatusDictionary ─────────────────────────────────────────────────────

    private static async Task SeedStatusDictionaryAsync(SuspenseManagerDbContext db)
    {
        if (await db.StatusDictionary.AnyAsync()) return;

        db.StatusDictionary.AddRange(
            new StatusDictionary { Code = 0,   Name = "Нет продукта",            Description = "Суспенс не в группе, продукт не найден в каталоге" },
            new StatusDictionary { Code = 1,   Name = "Нет прав",                Description = "Суспенс не в группе, продукт найден, права не определены" },
            new StatusDictionary { Code = 15,  Name = "В группе — нет продукта", Description = "Суспенс в группе, ожидает каталогизации" },
            new StatusDictionary { Code = 16,  Name = "В группе — нет прав",     Description = "Суспенс в группе, продукт привязан, ожидает прав" },
            new StatusDictionary { Code = 30,  Name = "Отложено — нет продукта", Description = "Группа без продукта отложена" },
            new StatusDictionary { Code = 32,  Name = "Отложено — нет прав",     Description = "Группа без прав отложена" },
            new StatusDictionary { Code = 88,  Name = "Валидировано",            Description = "Суспенс прошёл валидацию" },
            new StatusDictionary { Code = 120, Name = "Бэк-офис — нет продукта", Description = "Группа без продукта передана в бэк-офис" },
            new StatusDictionary { Code = 320, Name = "Бэк-офис — нет прав",     Description = "Группа без прав передана в бэк-офис" }
        );
        await db.SaveChangesAsync();
    }

    // ── Territories ──────────────────────────────────────────────────────────

    private static async Task SeedTerritoriesAsync(SuspenseManagerDbContext db)
    {
        var defs = new (string Code, string Name)[]
        {
            ("RU", "Россия"),         ("US", "США"),              ("GB", "Великобритания"),
            ("DE", "Германия"),       ("WW", "Весь мир"),
            // Европа
            ("FR", "Франция"),        ("IT", "Италия"),           ("ES", "Испания"),
            ("PL", "Польша"),         ("NL", "Нидерланды"),       ("SE", "Швеция"),
            ("NO", "Норвегия"),       ("FI", "Финляндия"),        ("DK", "Дания"),
            ("AT", "Австрия"),        ("CH", "Швейцария"),        ("BE", "Бельгия"),
            ("PT", "Португалия"),     ("CZ", "Чехия"),            ("SK", "Словакия"),
            ("HU", "Венгрия"),
            // СНГ
            ("KZ", "Казахстан"),      ("UA", "Украина"),          ("BY", "Беларусь"),
            ("UZ", "Узбекистан"),     ("AM", "Армения"),          ("AZ", "Азербайджан"),
            ("GE", "Грузия"),
        };

        var existing = await db.Territories.Select(t => t.TerritoryCode).ToHashSetAsync();
        var newOnes = defs
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new Territory { TerritoryCode = d.Code, TerritoryName = d.Name, CreateTime = DateTime.UtcNow })
            .ToList();

        if (newOnes.Count > 0)
        {
            db.Territories.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Companies ────────────────────────────────────────────────────────────

    private static async Task SeedCompaniesAsync(SuspenseManagerDbContext db)
    {
        var defs = new[]
        {
            new { Code = "MELODY",  Legal = "ООО Мелодия Рекордс",      Short = "Мелодия",    Bank = "Сбербанк",      Inn = "7701234567", Bic = "044525225", Country = "RU", Phone = "+74951234567", Addr = "г. Москва, ул. Тверская, д. 1"          },
            new { Code = "ZVUK",    Legal = "ООО Звук Дистрибьюция",    Short = "Звук",       Bank = "Тинькофф",      Inn = "7709876543", Bic = "044525974", Country = "RU", Phone = "+74959876543", Addr = "г. Москва, ул. Арбат, д. 10"             },
            new { Code = "FIRST",   Legal = "ООО Первое Музыкальное",   Short = "ПервоеМуз",  Bank = "ВТБ",           Inn = "7801112233", Bic = "044030702", Country = "RU", Phone = "+78121112233", Addr = "г. Санкт-Петербург, Невский пр., д. 5"   },
            new { Code = "SOYUZ",   Legal = "ООО Студия Союз",          Short = "Союз",       Bank = "Альфа-Банк",    Inn = "7712345678", Bic = "044525593", Country = "RU", Phone = "+74952345678", Addr = "г. Москва, ул. Садовая, д. 20"           },
            new { Code = "NATMUS",  Legal = "АО Национальная Музыка",   Short = "НацМуз",     Bank = "Газпромбанк",   Inn = "7734567890", Bic = "044525823", Country = "RU", Phone = "+74954567890", Addr = "г. Москва, Ленинский пр., д. 15"         },
            new { Code = "RECPUB",  Legal = "ООО Рекорд Паблишинг",     Short = "РекордПаб",  Bank = "Сбербанк",      Inn = "7723456789", Bic = "044525225", Country = "RU", Phone = "+74953456789", Addr = "г. Москва, ул. Пречистенка, д. 7"        },
            new { Code = "DIGISND", Legal = "ООО Цифровой Звук",        Short = "ЦифрЗвук",   Bank = "Тинькофф",      Inn = "7745678901", Bic = "044525974", Country = "RU", Phone = "+74955678901", Addr = "г. Москва, Кутузовский пр., д. 30"       },
            new { Code = "UMG",     Legal = "Universal Music Group",    Short = "UMG",        Bank = "Deutsche Bank", Inn = (string)null, Bic = (string)null, Country = "US", Phone = "+12125551000", Addr = "1755 Broadway, New York, NY 10019, USA"  },
            new { Code = "SONY",    Legal = "Sony Music Entertainment", Short = "Sony Music", Bank = "JP Morgan",     Inn = (string)null, Bic = (string)null, Country = "US", Phone = "+12125552000", Addr = "25 Madison Ave, New York, NY 10010, USA" },
            new { Code = "WMG",     Legal = "Warner Music Group",       Short = "Warner",     Bank = "Citibank",      Inn = (string)null, Bic = (string)null, Country = "US", Phone = "+12125553000", Addr = "1633 Broadway, New York, NY 10019, USA"  },
            new { Code = "BMG",     Legal = "BMG Rights Management",    Short = "BMG",        Bank = "Commerzbank",   Inn = (string)null, Bic = (string)null, Country = "DE", Phone = "+493015551000", Addr = "Weidestraße 122b, 22083 Hamburg, DE"    },
            new { Code = "PIAS",    Legal = "PIAS Entertainment Group", Short = "PIAS",       Bank = "ING Bank",      Inn = (string)null, Bic = (string)null, Country = "BE", Phone = "+3225551000",   Addr = "Rue de la Loi 200, 1040 Brussels, BE"   },
            new { Code = "BLIEVE",  Legal = "Believe Digital",          Short = "Believe",    Bank = "BNP Paribas",   Inn = (string)null, Bic = (string)null, Country = "FR", Phone = "+3315551000",   Addr = "24 Rue Toulouse Lautrec, 75017 Paris, FR"},
            new { Code = "KONTOR",  Legal = "Kontor Records",           Short = "Kontor",     Bank = "Deutsche Bank", Inn = (string)null, Bic = (string)null, Country = "DE", Phone = "+494015551000", Addr = "Valentinskamp 70, 20355 Hamburg, DE"    },
        };

        var existing = await db.Companies.Select(c => c.CompanyCode).ToHashSetAsync();
        var newOnes = defs
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new Company
            {
                CompanyCode = d.Code, LegalName = d.Legal, ShortName = d.Short,
                BankName = d.Bank, Inn = d.Inn, Bic = d.Bic, Country = d.Country,
                PhoneNumber = d.Phone, LegalAddress = d.Addr, ActualAddress = d.Addr,
                CreateTime = DateTime.UtcNow
            })
            .ToList();

        if (newOnes.Count > 0)
        {
            db.Companies.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Product types ────────────────────────────────────────────────────────

    private static async Task SeedProductTypesAsync(SuspenseManagerDbContext db)
    {
        var defs = new (string Code, string Desc)[]
        {
            ("CD", "Компакт-диск"), ("VINYL", "Виниловая пластинка"),
            ("DIGI", "Цифровой релиз"), ("CASS", "Кассета"),
        };

        var existing = await db.CatalogProductTypes.Select(t => t.Code).ToHashSetAsync();
        var newOnes = defs
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new CatalogProductType { Code = d.Code, Description = d.Desc, CreateTime = DateTime.UtcNow })
            .ToList();

        if (newOnes.Count > 0)
        {
            db.CatalogProductTypes.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Catalog products ─────────────────────────────────────────────────────

    private static async Task SeedProductsAsync(SuspenseManagerDbContext db)
    {
        var types = await db.CatalogProductTypes.ToDictionaryAsync(t => t.Code, t => t.Id);
        int digi = types["DIGI"], cd = types["CD"], vinyl = types["VINYL"];

        var defs = new[]
        {
            // Оригинальные 5
            new { Isrc = "RU-A0A-25-00001", Barcode = "4607012345678", CatNum = "CAT-001", Format = "DIGI", TypeId = digi, Name = "Летний вечер",      Artist = "Артём Иванов",    Album = "Лето 2025",      Genre = "Pop"        },
            new { Isrc = "RU-A0A-25-00002", Barcode = "4607098765432", CatNum = "CAT-002", Format = "DIGI", TypeId = digi, Name = "Ночной город",      Artist = "Мария Петрова",   Album = "Огни",           Genre = "Rock"       },
            new { Isrc = "US-A0B-25-00001", Barcode = "0012345678905", CatNum = "CAT-003", Format = "DIGI", TypeId = digi, Name = "Midnight Dreams",   Artist = "John Smith",      Album = "Night Sessions", Genre = "Electronic" },
            new { Isrc = "GB-A0C-25-00001", Barcode = "5012345678901", CatNum = "CAT-004", Format = "CD",   TypeId = cd,   Name = "Rainy Day",         Artist = "Emma Wilson",     Album = "Weather",        Genre = "Jazz"       },
            new { Isrc = "RU-A0A-25-00005", Barcode = "4607055555555", CatNum = "CAT-005", Format = "DIGI", TypeId = digi, Name = "Дорога домой",      Artist = "Группа Путь",     Album = "Путешествие",    Genre = "Rock"       },
            // Новые 15 с правами
            new { Isrc = "RU-A0A-25-00006", Barcode = "4607011110001", CatNum = "CAT-006", Format = "DIGI", TypeId = digi, Name = "Белые ночи",        Artist = "Нева Рекордс",    Album = "Петербург",      Genre = "Pop"        },
            new { Isrc = "RU-A0A-25-00007", Barcode = "4607011110002", CatNum = "CAT-007", Format = "DIGI", TypeId = digi, Name = "Магнит",            Artist = "Полина Сова",     Album = "Притяжение",     Genre = "Pop"        },
            new { Isrc = "RU-A0A-25-00008", Barcode = "4607011110003", CatNum = "CAT-008", Format = "DIGI", TypeId = digi, Name = "Холодный фронт",    Artist = "Арктика",         Album = "Зима 2025",      Genre = "Rock"       },
            new { Isrc = "RU-A0A-25-00009", Barcode = "4607011110004", CatNum = "CAT-009", Format = "DIGI", TypeId = digi, Name = "Шёпот листьев",     Artist = "Лесной оркестр",  Album = "Осень",          Genre = "Jazz"       },
            new { Isrc = "RU-A0A-25-00010", Barcode = "4607011110005", CatNum = "CAT-010", Format = "DIGI", TypeId = digi, Name = "Скорость света",    Artist = "Фотон",           Album = "Квант",          Genre = "Electronic" },
            new { Isrc = "DE-A0D-25-00001", Barcode = "4006001100001", CatNum = "CAT-011", Format = "CD",   TypeId = cd,   Name = "Herbstlicht",       Artist = "Klaus Weber",     Album = "Jahreszeiten",   Genre = "Classical"  },
            new { Isrc = "FR-A0E-25-00001", Barcode = "3700000100001", CatNum = "CAT-012", Format = "DIGI", TypeId = digi, Name = "Minuit à Paris",    Artist = "Jean Dubois",     Album = "La Ville",       Genre = "Jazz"       },
            new { Isrc = "SE-A0F-25-00001", Barcode = "7320000100001", CatNum = "CAT-013", Format = "VINYL",TypeId = vinyl, Name = "Nordlys",           Artist = "Erik Strand",     Album = "Skandinavien",   Genre = "Folk"       },
            new { Isrc = "RU-A0A-25-00014", Barcode = "4607011110009", CatNum = "CAT-014", Format = "DIGI", TypeId = digi, Name = "Город и река",      Artist = "Волга Бэнд",      Album = "Течение",        Genre = "Pop"        },
            new { Isrc = "RU-A0A-25-00015", Barcode = "4607011110010", CatNum = "CAT-015", Format = "DIGI", TypeId = digi, Name = "Пульс",             Artist = "Ритм Коллектив",  Album = "Удар",           Genre = "Hip-Hop"    },
            new { Isrc = "GB-A0C-25-00002", Barcode = "5012345679001", CatNum = "CAT-016", Format = "DIGI", TypeId = digi, Name = "Silver Rain",       Artist = "The Wanderers",   Album = "Crossroads",     Genre = "Rock"       },
            new { Isrc = "US-A0B-25-00002", Barcode = "0012345679001", CatNum = "CAT-017", Format = "DIGI", TypeId = digi, Name = "Deep Blue",         Artist = "Marcus Cole",     Album = "Ocean",          Genre = "Electronic" },
            new { Isrc = "RU-A0A-25-00018", Barcode = "4607011110013", CatNum = "CAT-018", Format = "DIGI", TypeId = digi, Name = "Тихий час",         Artist = "Соня Громова",    Album = "Полдень",        Genre = "Pop"        },
            new { Isrc = "RU-A0A-25-00019", Barcode = "4607011110014", CatNum = "CAT-019", Format = "DIGI", TypeId = digi, Name = "Огонь и лёд",       Artist = "Контраст",        Album = "Стихии",         Genre = "Rock"       },
            new { Isrc = "IT-A0G-25-00001", Barcode = "8030000100001", CatNum = "CAT-020", Format = "CD",   TypeId = cd,   Name = "Sole d'Estate",     Artist = "Marco Rossi",     Album = "Estate",         Genre = "Pop"        },
            // 5 без прав
            new { Isrc = "RU-A0A-25-00021", Barcode = "4607011110016", CatNum = "CAT-021", Format = "DIGI", TypeId = digi, Name = "Туман",             Artist = "Серая Зона",      Album = "Невидимость",    Genre = "Electronic" },
            new { Isrc = "RU-A0A-25-00022", Barcode = "4607011110017", CatNum = "CAT-022", Format = "DIGI", TypeId = digi, Name = "Пустота",           Artist = "Вакуум",          Album = "Тишина",         Genre = "Ambient"    },
            new { Isrc = "RU-A0A-25-00023", Barcode = "4607011110018", CatNum = "CAT-023", Format = "DIGI", TypeId = digi, Name = "Нулевой меридиан",  Artist = "Точка Отсчёта",   Album = "Начало",         Genre = "Rock"       },
            new { Isrc = "DE-A0D-25-00002", Barcode = "4006001100002", CatNum = "CAT-024", Format = "DIGI", TypeId = digi, Name = "Leere Straßen",     Artist = "Stadt Klang",     Album = "Nacht",          Genre = "Electronic" },
            new { Isrc = "FR-A0E-25-00002", Barcode = "3700000100002", CatNum = "CAT-025", Format = "DIGI", TypeId = digi, Name = "L'Absence",         Artist = "Claire Morin",    Album = "Vide",           Genre = "Pop"        },
        };

        var existing = await db.CatalogProducts.Select(p => p.Isrc).ToHashSetAsync();
        var newOnes = defs
            .Where(d => !existing.Contains(d.Isrc))
            .Select(d => new CatalogProduct
            {
                Isrc = d.Isrc, Barcode = d.Barcode, CatalogNumber = d.CatNum,
                ProductFormatCode = d.Format, ProductTypeId = d.TypeId,
                ProductName = d.Name, Artist = d.Artist, AlbumName = d.Album,
                Genre = d.Genre, CreateTime = DateTime.UtcNow
            })
            .ToList();

        if (newOnes.Count > 0)
        {
            db.CatalogProducts.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Product rights ───────────────────────────────────────────────────────

    private static async Task SeedProductRightsAsync(SuspenseManagerDbContext db)
    {
        var products    = await db.CatalogProducts.ToDictionaryAsync(p => p.Isrc,         p => p.Id);
        var companies   = await db.Companies.ToDictionaryAsync(c => c.CompanyCode,        c => c);
        var territories = await db.Territories.ToDictionaryAsync(t => t.TerritoryCode,    t => t);

        var defs = new[]
        {
            new { Isrc = "RU-A0A-25-00001", Sender = "MELODY",  Receiver = "ZVUK",    Doc = "DOC-2025-001",  Terr = "RU", Share = 50.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "RU-A0A-25-00002", Sender = "FIRST",   Receiver = "ZVUK",    Doc = "DOC-2025-002",  Terr = "RU", Share = 70.0, Start = new DateOnly(2025,3,1), End = new DateOnly(2026,12,31) },
            new { Isrc = "US-A0B-25-00001", Sender = "UMG",     Receiver = "SONY",    Doc = "DOC-2025-003",  Terr = "US", Share = 60.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "RU-A0A-25-00006", Sender = "MELODY",  Receiver = "ZVUK",    Doc = "DOC-2025-006",  Terr = "RU", Share = 55.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "RU-A0A-25-00007", Sender = "SOYUZ",   Receiver = "FIRST",   Doc = "DOC-2025-007",  Terr = "RU", Share = 65.0, Start = new DateOnly(2025,2,1), End = new DateOnly(2027,6,30)  },
            new { Isrc = "RU-A0A-25-00008", Sender = "NATMUS",  Receiver = "DIGISND", Doc = "DOC-2025-008",  Terr = "RU", Share = 50.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "RU-A0A-25-00008", Sender = "NATMUS",  Receiver = "DIGISND", Doc = "DOC-2025-008B", Terr = "BY", Share = 50.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "RU-A0A-25-00009", Sender = "RECPUB",  Receiver = "ZVUK",    Doc = "DOC-2025-009",  Terr = "RU", Share = 75.0, Start = new DateOnly(2024,6,1), End = new DateOnly(2026,5,31)  },
            new { Isrc = "RU-A0A-25-00010", Sender = "DIGISND", Receiver = "FIRST",   Doc = "DOC-2025-010",  Terr = "RU", Share = 60.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "DE-A0D-25-00001", Sender = "BMG",     Receiver = "WMG",     Doc = "DOC-2025-011",  Terr = "DE", Share = 50.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "FR-A0E-25-00001", Sender = "BLIEVE",  Receiver = "UMG",     Doc = "DOC-2025-012",  Terr = "FR", Share = 55.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "SE-A0F-25-00001", Sender = "WMG",     Receiver = "SONY",    Doc = "DOC-2025-013",  Terr = "SE", Share = 60.0, Start = new DateOnly(2025,3,1), End = new DateOnly(2028,3,1)   },
            new { Isrc = "RU-A0A-25-00014", Sender = "MELODY",  Receiver = "SOYUZ",   Doc = "DOC-2025-014",  Terr = "RU", Share = 70.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2026,12,31) },
            new { Isrc = "RU-A0A-25-00015", Sender = "NATMUS",  Receiver = "ZVUK",    Doc = "DOC-2025-015",  Terr = "RU", Share = 65.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "GB-A0C-25-00002", Sender = "UMG",     Receiver = "WMG",     Doc = "DOC-2025-016",  Terr = "GB", Share = 50.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "US-A0B-25-00002", Sender = "SONY",    Receiver = "UMG",     Doc = "DOC-2025-017",  Terr = "US", Share = 55.0, Start = new DateOnly(2025,2,1), End = new DateOnly(2028,12,31) },
            new { Isrc = "RU-A0A-25-00018", Sender = "FIRST",   Receiver = "MELODY",  Doc = "DOC-2025-018",  Terr = "RU", Share = 60.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2027,12,31) },
            new { Isrc = "RU-A0A-25-00019", Sender = "SOYUZ",   Receiver = "NATMUS",  Doc = "DOC-2025-019",  Terr = "RU", Share = 50.0, Start = new DateOnly(2025,4,1), End = new DateOnly(2028,4,1)   },
            new { Isrc = "IT-A0G-25-00001", Sender = "UMG",     Receiver = "SONY",    Doc = "DOC-2025-020",  Terr = "IT", Share = 60.0, Start = new DateOnly(2025,1,1), End = new DateOnly(2028,12,31) },
        };

        var existingKeys = await db.CatalogProductRights
            .Select(r => new { r.CatalogProductId, r.DocNumber, r.TerritoryCode })
            .ToListAsync();
        var existingSet = existingKeys
            .Select(r => (r.CatalogProductId, r.DocNumber, r.TerritoryCode))
            .ToHashSet();

        var newOnes = new List<CatalogProductRights>();
        foreach (var d in defs)
        {
            if (!products.TryGetValue(d.Isrc, out var productId)) continue;
            if (!companies.TryGetValue(d.Sender, out var sender)) continue;
            if (!companies.TryGetValue(d.Receiver, out var receiver)) continue;
            if (!territories.TryGetValue(d.Terr, out var territory)) continue;
            if (existingSet.Contains((productId, d.Doc, d.Terr))) continue;

            newOnes.Add(new CatalogProductRights
            {
                CatalogProductId  = productId,
                CompanySender     = sender.ShortName,   CompanySenderId   = sender.Id,
                CompanyReceiver   = receiver.ShortName, CompanyReceiverId = receiver.Id,
                DocNumber         = d.Doc,
                TerritoryCode     = d.Terr,             TerritoryDesc     = territory.TerritoryName,
                TerritoryId       = territory.Id,
                Share             = d.Share,
                DocStart          = d.Start,            DocEnd            = d.End,
                CreateTime        = DateTime.UtcNow
            });
        }

        if (newOnes.Count > 0)
        {
            db.CatalogProductRights.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Admin account ────────────────────────────────────────────────────────

    private static async Task SeedAdminAccountAsync(SuspenseManagerDbContext db, string adminPasswordHash)
    {
        if (await db.Accounts.AnyAsync(a => a.Login == "admin")) return;

        var user = new User
        {
            Name = "Админ", Surname = "Системный", MiddleName = "Тестович",
            Email = "admin@suspense.local", PhoneNumber = "+70001234567",
            Position = "Администратор", CreateTime = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Accounts.Add(new Account
        {
            Login = "admin", PasswordHash = adminPasswordHash,
            Description = "Тестовый аккаунт администратора",
            UserId = user.Id, CreateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // ── Permissions ──────────────────────────────────────────────────────────

    private static async Task SeedPermissionsAsync(SuspenseManagerDbContext db)
    {
        var defs = new[]
        {
            // Загрузка
            new { Code = "uploads.view",                        Name = "Просмотр истории загрузок",      Module = "Загрузка"            },
            new { Code = "uploads.create",                      Name = "Загрузка файлов",                Module = "Загрузка"            },
            // Группировка
            new { Code = "grouping.view",                       Name = "Просмотр группировки",           Module = "Группировка"         },
            new { Code = "grouping.create",                     Name = "Создание групп",                 Module = "Группировка"         },
            // Группы — нет продукта
            new { Code = "groups.no_product.view",              Name = "Просмотр групп (нет продукта)",  Module = "Группы нет продукта" },
            new { Code = "groups.no_product.catalog_fast",      Name = "Быстрая каталогизация",          Module = "Группы нет продукта" },
            new { Code = "groups.no_product.possible_products", Name = "Поиск возможных продуктов",      Module = "Группы нет продукта" },
            new { Code = "groups.no_product.send_backoffice",   Name = "Отправить в бэк-офис",           Module = "Группы нет продукта" },
            new { Code = "groups.no_product.postpone",          Name = "Отложить группу",                Module = "Группы нет продукта" },
            new { Code = "groups.no_product.ungroup",           Name = "Разгруппировать",                Module = "Группы нет продукта" },
            // Группы — нет прав
            new { Code = "groups.no_rights.view",               Name = "Просмотр групп (нет прав)",      Module = "Группы нет прав"     },
            new { Code = "groups.no_rights.correct_rights",     Name = "Коррекция прав",                 Module = "Группы нет прав"     },
            new { Code = "groups.no_rights.send_backoffice",    Name = "Отправить в бэк-офис",           Module = "Группы нет прав"     },
            new { Code = "groups.no_rights.postpone",           Name = "Отложить группу",                Module = "Группы нет прав"     },
            new { Code = "groups.no_rights.ungroup",            Name = "Разгруппировать",                Module = "Группы нет прав"     },
            // Отложенные
            new { Code = "postponed.view",                      Name = "Просмотр отложенных групп",      Module = "Отложенные"          },
            new { Code = "postponed.return",                    Name = "Вернуть группу из отложенных",   Module = "Отложенные"          },
            // Бэк-офис
            new { Code = "backoffice.view",                     Name = "Просмотр заданий бэк-офиса",     Module = "Бэк-офис"            },
            new { Code = "backoffice.return",                   Name = "Вернуть группу оператору",       Module = "Бэк-офис"            },
            new { Code = "backoffice.validate",                 Name = "Валидировать группу",            Module = "Бэк-офис"            },
            new { Code = "backoffice.delete",                   Name = "Удалить группу",                 Module = "Бэк-офис"            },
            new { Code = "backoffice.link_product",             Name = "Привязать продукт",              Module = "Бэк-офис"            },
            new { Code = "backoffice.copy_rights",              Name = "Скопировать права",              Module = "Бэк-офис"            },
            // Каталог
            new { Code = "catalog.view",                        Name = "Просмотр каталога",              Module = "Каталог"             },
            new { Code = "catalog.products.edit",               Name = "Редактирование продуктов",       Module = "Каталог"             },
            new { Code = "catalog.rights.edit",                 Name = "Редактирование прав",            Module = "Каталог"             },
            new { Code = "catalog.companies.edit",              Name = "Редактирование компаний",        Module = "Каталог"             },
            new { Code = "catalog.territories.edit",            Name = "Редактирование территорий",      Module = "Каталог"             },
            // Экспорт
            new { Code = "groups.export",                       Name = "Экспорт групп и суспенсов",      Module = "Экспорт"             },
            // Мониторинг
            new { Code = "monitor.view",                        Name = "Мониторинг операторов",          Module = "Мониторинг"          },
            // Администрирование
            new { Code = "admin.users.manage",                  Name = "Управление пользователями",      Module = "Администрирование"   },
            new { Code = "admin.permissions.manage",            Name = "Управление правами",             Module = "Администрирование"   },
        };

        var existing = await db.Rights.Select(r => r.Code).ToHashSetAsync();
        var newOnes = defs
            .Where(d => !existing.Contains(d.Code))
            .Select(d => new Rights { Code = d.Code, Name = d.Name, Module = d.Module, CreateTime = DateTime.UtcNow })
            .ToList();

        if (newOnes.Count > 0)
        {
            db.Rights.AddRange(newOnes);
            await db.SaveChangesAsync();
        }
    }

    // ── Admin gets all permissions automatically ──────────────────────────────

    private static async Task SeedAdminPermissionLinksAsync(SuspenseManagerDbContext db)
    {
        var admin = await db.Accounts.FirstOrDefaultAsync(a => a.Login == "admin");
        if (admin is null) return;

        var linked = await db.AccountRightsLinks
            .Where(l => l.AccountId == admin.Id)
            .Select(l => l.RightId)
            .ToHashSetAsync();

        var allRights = await db.Rights.ToListAsync();
        var newLinks = allRights
            .Where(r => !linked.Contains(r.Id))
            .Select(r => new AccountRightsLink { AccountId = admin.Id, RightId = r.Id, CreateTime = DateTime.UtcNow })
            .ToList();

        if (newLinks.Count > 0)
        {
            db.AccountRightsLinks.AddRange(newLinks);
            await db.SaveChangesAsync();
        }
    }

    // ── Operator account ─────────────────────────────────────────────────────
    // Логин: operator / Пароль: Operator123!
    // Права: всё кроме admin.*, backoffice.*, catalog.*, monitor.view

    private static async Task SeedOperatorAccountAsync(SuspenseManagerDbContext db, string passwordHash)
    {
        if (await db.Accounts.AnyAsync(a => a.Login == "operator")) return;

        var user = new User
        {
            Name = "Алексей", Surname = "Операторов", MiddleName = "Сергеевич",
            Email = "operator@suspense.local", PhoneNumber = "+70009876543",
            Position = "Оператор обработки суспенсов", CreateTime = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hash = passwordHash;
        var account = new Account
        {
            Login = "operator", PasswordHash = hash,
            Description = "Тестовый аккаунт оператора",
            UserId = user.Id, CreateTime = DateTime.UtcNow
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var allRights = await db.Rights.ToListAsync();
        var operatorRights = allRights
            .Where(r =>
                !r.Code.StartsWith("admin.", StringComparison.Ordinal) &&
                !r.Code.StartsWith("backoffice.", StringComparison.Ordinal) &&
                !r.Code.StartsWith("catalog.", StringComparison.Ordinal) &&
                r.Code != "monitor.view")
            .Select(r => new AccountRightsLink { AccountId = account.Id, RightId = r.Id, CreateTime = DateTime.UtcNow })
            .ToList();

        db.AccountRightsLinks.AddRange(operatorRights);
        await db.SaveChangesAsync();
    }

    // ── Back-office account ──────────────────────────────────────────────────
    // Логин: backoffice / Пароль: Backoffice123!
    // Права: backoffice.*, catalog.*, monitor.view

    private static async Task SeedBackOfficeAccountAsync(SuspenseManagerDbContext db, string passwordHash)
    {
        if (await db.Accounts.AnyAsync(a => a.Login == "backoffice")) return;

        var user = new User
        {
            Name = "Елена", Surname = "Бэкофисова", MiddleName = "Ивановна",
            Email = "backoffice@suspense.local", PhoneNumber = "+70005554433",
            Position = "Сотрудник бэк-офиса", CreateTime = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hash = passwordHash;
        var account = new Account
        {
            Login = "backoffice", PasswordHash = hash,
            Description = "Тестовый аккаунт сотрудника бэк-офиса",
            UserId = user.Id, CreateTime = DateTime.UtcNow
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var allRights = await db.Rights.ToListAsync();
        var boRights = allRights
            .Where(r =>
                r.Code.StartsWith("backoffice.", StringComparison.Ordinal) ||
                r.Code.StartsWith("catalog.", StringComparison.Ordinal) ||
                r.Code == "monitor.view")
            .Select(r => new AccountRightsLink { AccountId = account.Id, RightId = r.Id, CreateTime = DateTime.UtcNow })
            .ToList();

        db.AccountRightsLinks.AddRange(boRights);
        await db.SaveChangesAsync();
    }

    // ── Demo suspense data ────────────────────────────────────────────────────
    // Создаёт реалистичный набор данных для демонстрации всех функций системы.
    // Запускается только один раз — пропускается если группы уже есть.

    private static async Task SeedDemoDataAsync(SuspenseManagerDbContext db)
    {
        if (await db.SuspenseGroups.AnyAsync()) return;

        var opAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Login == "operator");
        var adminAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Login == "admin");
        if (opAccount is null || adminAccount is null) return;

        int opId = opAccount.Id;
        int admId = adminAccount.Id;

        var products = await db.CatalogProducts.ToDictionaryAsync(p => p.Isrc!, p => p.Id);

        // ── Группа 1: статус 15 (в работе, нет продукта) — 4 суспенса ──────────
        var g1 = await CreateGroupAsync(db, 15, opId, DateTime.UtcNow.AddDays(-8));
        var lines1 = new[]
        {
            MakeLine(15, g1.Id, null, "XX-GRP1-25-001", null, "Неизвестная Группа", "Трек 1", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "DEMO-NP-001", "RU", "Pop", 8500, 0.004, "RUB", 1.0m, -8),
            MakeLine(15, g1.Id, null, "XX-GRP1-25-002", null, "Неизвестная Группа", "Трек 2", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "DEMO-NP-001", "RU", "Pop", 6200, 0.004, "RUB", 1.0m, -7),
            MakeLine(15, g1.Id, null, "XX-GRP1-25-003", null, "Неизвестная Группа", "Трек 3", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "DEMO-NP-001", "RU", "Pop", 9100, 0.004, "RUB", 1.0m, -7),
            MakeLine(15, g1.Id, null, "XX-GRP1-25-004", null, "Неизвестная Группа", "Трек 4", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "DEMO-NP-001", "RU", "Pop", 4300, 0.004, "RUB", 1.0m, -6),
        };
        await SaveLinesAndLinksAsync(db, lines1, g1.Id, opId, 15);

        // ── Группа 2: статус 16 (в работе, нет прав) — 3 суспенса ──────────────
        int? productId21 = products.TryGetValue("RU-A0A-25-00021", out var pid21) ? pid21 : null;
        var g2 = await CreateGroupAsync(db, 16, opId, DateTime.UtcNow.AddDays(-5), productId21);
        var lines2 = new[]
        {
            MakeLine(16, g2.Id, productId21, "RU-A0A-25-00021", "4607011110016", "Серая Зона", "Туман", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "DEMO-NR-001", "RU", "Electronic", 3200, 0.003, "RUB", 1.0m, -5),
            MakeLine(16, g2.Id, productId21, "RU-A0A-25-00021", "4607011110016", "Серая Зона", "Туман", "Spotify",       "Мелодия", "Звук", "Лицензионный", "DEMO-NR-001", "RU", "Electronic", 2800, 0.0032, "RUB", 1.0m, -4),
            MakeLine(16, g2.Id, productId21, "RU-A0A-25-00021", "4607011110016", "Серая Зона", "Туман", "Apple Music",   "Мелодия", "Звук", "Лицензионный", "DEMO-NR-001", "RU", "Electronic", 1900, 0.0035, "RUB", 1.0m, -4),
        };
        await SaveLinesAndLinksAsync(db, lines2, g2.Id, opId, 16);

        // ── Группа 3: статус 30 (отложено, нет продукта) — 2 суспенса ──────────
        var g3 = await CreateGroupAsync(db, 30, opId, DateTime.UtcNow.AddDays(-12), postponeReason: "Ожидаем данные от дистрибьютора");
        var lines3 = new[]
        {
            MakeLine(30, g3.Id, null, "XX-POST-25-001", null, "Отложенный Артист", "Трек A", "Яндекс Музыка", "Союз", "ПервоеМуз", "Лицензионный", "DEMO-POST-001", "RU", "Rock", 5500, 0.0038, "RUB", 1.0m, -12),
            MakeLine(30, g3.Id, null, "XX-POST-25-002", null, "Отложенный Артист", "Трек B", "Spotify",       "Союз", "ПервоеМуз", "Лицензионный", "DEMO-POST-001", "RU", "Rock", 3800, 0.0038, "RUB", 1.0m, -11),
        };
        await SaveLinesAndLinksAsync(db, lines3, g3.Id, opId, 30);

        // ── Группа 4: статус 120 (бэк-офис, нет продукта) — 2 суспенса ─────────
        var g4 = await CreateGroupAsync(db, 120, opId, DateTime.UtcNow.AddDays(-15));
        var lines4 = new[]
        {
            MakeLine(120, g4.Id, null, "XX-BO-25-001", null, "Сложный Кейс", "Трек X", "Яндекс Музыка", "НацМуз", "ЦифрЗвук", "Лицензионный", "DEMO-BO-001", "RU", "Pop", 12000, 0.005, "RUB", 1.0m, -15),
            MakeLine(120, g4.Id, null, "XX-BO-25-002", null, "Сложный Кейс", "Трек Y", "Spotify",       "НацМуз", "ЦифрЗвук", "Лицензионный", "DEMO-BO-001", "RU", "Pop",  8700, 0.005, "RUB", 1.0m, -14),
        };
        await SaveLinesAndLinksAsync(db, lines4, g4.Id, admId, 120);

        // ── Свободные суспенсы статус 0 (не в группах — для демонстрации группировки) ──
        var freeNoProduct = new[]
        {
            MakeLine(0, null, null, "XX-FREE-25-001", null, "Группа Без Имени", "Сингл 1", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "FREE-NP-001", "RU", "Pop",  4200, 0.004, "RUB", 1.0m, -3),
            MakeLine(0, null, null, "XX-FREE-25-002", null, "Группа Без Имени", "Сингл 2", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "FREE-NP-001", "RU", "Pop",  3700, 0.004, "RUB", 1.0m, -3),
            MakeLine(0, null, null, "XX-FREE-25-003", null, "Группа Без Имени", "Сингл 3", "Яндекс Музыка", "Мелодия", "Звук", "Лицензионный", "FREE-NP-001", "RU", "Pop",  5100, 0.004, "RUB", 1.0m, -2),
            MakeLine(0, null, null, "XX-FREE-25-004", null, "Группа Без Имени", "Сингл 4", "Spotify",       "Мелодия", "Звук", "Лицензионный", "FREE-NP-001", "RU", "Pop",  2900, 0.0042, "RUB", 1.0m, -2),
            MakeLine(0, null, null, "XX-FREE-25-005", null, "Группа Без Имени", "Сингл 5", "Apple Music",   "Мелодия", "Звук", "Лицензионный", "FREE-NP-001", "RU", "Pop",  6600, 0.0038, "RUB", 1.0m, -1),
        };
        db.SuspenseLines.AddRange(freeNoProduct);

        // ── Свободные суспенсы статус 1 (не в группах — для демонстрации группировки) ──
        int? pid22 = products.TryGetValue("RU-A0A-25-00022", out var p22) ? p22 : null;
        int? pid23 = products.TryGetValue("RU-A0A-25-00023", out var p23) ? p23 : null;
        int? pidDe = products.TryGetValue("DE-A0D-25-00002", out var pDe) ? pDe : null;
        int? pidFr = products.TryGetValue("FR-A0E-25-00002", out var pFr) ? pFr : null;
        int? pid25 = products.TryGetValue("RU-A0A-25-00022", out var p25) ? p25 : null;

        var freeNoRights = new[]
        {
            MakeLine(1, null, pid22, "RU-A0A-25-00022", "4607011110017", "Вакуум", "Пустота",         "Яндекс Музыка", "Мелодия",  "Звук",       "Лицензионный", "FREE-NR-001", "RU", "Ambient",    1500, 0.003,  "RUB", 1.0m, -3),
            MakeLine(1, null, pid23, "RU-A0A-25-00023", "4607011110018", "Точка Отсчёта", "Нулевой меридиан", "Spotify", "НацМуз", "ЦифрЗвук",  "Лицензионный", "FREE-NR-002", "RU", "Rock",       5100, 0.004,  "RUB", 1.0m, -2),
            MakeLine(1, null, pidDe, "DE-A0D-25-00002", "4006001100002", "Stadt Klang",  "Leere Straßen",    "Spotify", "BMG",    "WMG",        "License",       "FREE-NR-003", "DE", "Electronic", 7800, 0.0042, "EUR", 100.3m, -2),
            MakeLine(1, null, pidFr, "FR-A0E-25-00002", "3700000100002", "Claire Morin", "L'Absence",        "Apple Music", "PIAS", "UMG",       "License",       "FREE-NR-004", "FR", "Pop",        3300, 0.0039, "EUR", 100.3m, -1),
            MakeLine(1, null, pid22, "RU-A0A-25-00022", "4607011110017", "Вакуум",       "Пустота",          "Apple Music", "Мелодия", "Звук",    "Лицензионный", "FREE-NR-001", "RU", "Ambient",    2200, 0.003,  "RUB", 1.0m, -1),
        };
        db.SuspenseLines.AddRange(freeNoRights);

        await db.SaveChangesAsync();
    }

    private static async Task<SuspenseGroup> CreateGroupAsync(
        SuspenseManagerDbContext db,
        int status,
        int accountId,
        DateTime createTime,
        int? catalogProductId = null,
        string? postponeReason = null)
    {
        var group = new SuspenseGroup
        {
            BusinessStatus = status,
            AccountId = accountId,
            CatalogProductId = catalogProductId,
            PostponeReason = postponeReason,
            ArchiveLevel = 0,
            CreateTime = createTime,
            ChangeTime = createTime
        };
        db.SuspenseGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private static async Task SaveLinesAndLinksAsync(
        SuspenseManagerDbContext db,
        SuspenseLine[] lines,
        int groupId,
        int accountId,
        int status)
    {
        db.SuspenseLines.AddRange(lines);
        await db.SaveChangesAsync();

        var links = lines.Select(l => new SuspenseGroupLink
        {
            SuspenseId = l.Id,
            SuspenseGroupId = groupId,
            AccountId = accountId,
            BusinessStatus = status,
            CreateTime = l.CreateTime,
            ChangeTime = l.CreateTime
        });
        db.SuspenseGroupLinks.AddRange(links);
        await db.SaveChangesAsync();
    }

    private static SuspenseLine MakeLine(
        int status, int? groupId, int? productId,
        string? isrc, string? barcode,
        string artist, string title,
        string op, string sender, string recipient,
        string agType, string agNumber, string territory,
        string genre, int qty, double ppd, string currency, decimal rate,
        int daysAgo)
    {
        return new SuspenseLine
        {
            BusinessStatus = status,
            GroupId = groupId,
            ProductId = productId,
            Isrc = isrc,
            Barcode = barcode,
            Artist = artist,
            TrackTitle = title,
            Operator = op,
            SenderCompany = sender,
            RecipientCompany = recipient,
            AgreementType = agType,
            AgreementNumber = agNumber,
            TerritoryCode = territory,
            Genre = genre,
            Qty = qty,
            Ppd = ppd,
            ExchangeCurrency = currency,
            ExchangeRate = rate,
            CauseSuspense = status == 0
                ? "Продукт не найден в каталоге"
                : "Продукт найден, права не определены",
            CreateTime = DateTime.UtcNow.AddDays(daysAgo),
            ArchiveLevel = 0
        };
    }
}
