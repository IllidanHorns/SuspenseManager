using Data;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Integration.Fixtures;

/// <summary>
/// Построитель тестовых данных для интеграционных тестов.
/// </summary>
public class TestDataBuilder
{
    private readonly SuspenseManagerDbContext _db;

    public TestDataBuilder(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<Account> CreateAccountAsync(
        string login = "testadmin",
        string password = "Admin123!",
        IEnumerable<string>? permissions = null)
    {
        var account = new Account
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            ArchiveLevel = 0,
            CreateTime = DateTime.UtcNow
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        if (permissions != null)
        {
            foreach (var code in permissions)
            {
                var right = new Rights
                {
                    Code = code,
                    Name = code,
                    CreateTime = DateTime.UtcNow
                };
                _db.Rights.Add(right);
                await _db.SaveChangesAsync();

                _db.AccountRightsLinks.Add(new AccountRightsLink
                {
                    AccountId = account.Id,
                    RightId = right.Id,
                    ArchiveLevel = 0,
                    CreateTime = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }

        return account;
    }

    public async Task<CatalogProduct> CreateProductAsync(
        string isrc = "ISRC001",
        string barcode = "1234567890",
        string catalogNumber = "CAT-001",
        string formatCode = "DIGI",
        string productName = "Test Album",
        string artist = "Test Artist")
    {
        var productType = await GetOrCreateProductTypeAsync(formatCode);

        var product = new CatalogProduct
        {
            Isrc = isrc,
            Barcode = barcode,
            CatalogNumber = catalogNumber,
            ProductFormatCode = formatCode,
            ProductName = productName,
            Artist = artist,
            ProductTypeId = productType.Id,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.CatalogProducts.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<CatalogProductRights> CreateRightsAsync(
        int productId,
        string docNumber = "AGR-001",
        string territory = "RU",
        string sender = "Sender LLC",
        string receiver = "Recipient LLC")
    {
        var rights = new CatalogProductRights
        {
            CatalogProductId = productId,
            DocNumber = docNumber,
            TerritoryCode = territory,
            TerritoryDesc = territory,
            CompanySender = sender,
            CompanyReceiver = receiver,
            DocStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            DocEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.CatalogProductRights.Add(rights);
        await _db.SaveChangesAsync();
        return rights;
    }

    public async Task<SuspenseLine> CreateSuspenseLineAsync(
        int status = (int)BusinessStatus.NoProduct,
        string artist = "Artist",
        string territory = "RU",
        int? groupId = null,
        int? productId = null)
    {
        var line = new SuspenseLine
        {
            BusinessStatus = status,
            Artist = artist,
            TerritoryCode = territory,
            Isrc = "ISRC001",
            Barcode = "1234567890",
            CatalogNumber = "CAT-001",
            AgreementNumber = "AGR-001",
            SenderCompany = "Sender LLC",
            RecipientCompany = "Recipient LLC",
            GroupId = groupId,
            ProductId = productId,
            Qty = 100,
            ExchangeCurrency = 1m,
            ExchangeRate = 1m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.SuspenseLines.Add(line);
        await _db.SaveChangesAsync();
        return line;
    }

    public async Task<SuspenseGroup> CreateGroupAsync(
        int status = (int)BusinessStatus.InGroupNoProduct,
        int accountId = 1,
        int? productId = null)
    {
        var group = new SuspenseGroup
        {
            BusinessStatus = status,
            AccountId = accountId,
            CatalogProductId = productId,
            CreateTime = DateTime.UtcNow,
            ChangeTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.SuspenseGroups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    private async Task<CatalogProductType> GetOrCreateProductTypeAsync(string code)
    {
        var existing = _db.CatalogProductTypes.FirstOrDefault(t => t.Code == code);
        if (existing != null) return existing;

        var type = new CatalogProductType
        {
            Code = code,
            Description = code,
            CreateTime = DateTime.UtcNow
        };
        _db.CatalogProductTypes.Add(type);
        await _db.SaveChangesAsync();
        return type;
    }
}
