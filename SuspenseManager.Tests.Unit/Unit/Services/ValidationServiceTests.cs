using Application.Services;
using Common.DTOs;
using Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Unit.Unit.Services;

/// <summary>
/// Модульные тесты ValidationService.
/// Проверяют логику поиска продукта, прав и присвоения статусов суспенсам.
/// </summary>
public class ValidationServiceTests : IDisposable
{
    private readonly SuspenseManagerDbContext _db;
    private readonly ValidationService _service;

    public ValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SuspenseManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SuspenseManagerDbContext(options);
        _service = new ValidationService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ──────────────────────────── Helpers ────────────────────────────────────

    private static SuspenseLineDto MakeDto(
        string isrc = "ISRC001",
        string barcode = "1234567890",
        string catalogNumber = "CAT-001",
        string productFormatCode = "DIGI",
        string agreementNumber = "AGR-001",
        string territoryCode = "RU",
        string senderCompany = "Sender LLC",
        string recipientCompany = "Recipient LLC") => new()
    {
        Isrc = isrc,
        Barcode = barcode,
        CatalogNumber = catalogNumber,
        ProductFormatCode = productFormatCode,
        AgreementNumber = agreementNumber,
        TerritoryCode = territoryCode,
        SenderCompany = senderCompany,
        RecipientCompany = recipientCompany,
        Artist = "Test Artist",
        TrackTitle = "Test Track",
        Qty = 100,
        ExchangeCurrency = 1m,
        ExchangeRate = 1.0m
    };

    private CatalogProduct MakeProduct(
        string isrc = "ISRC001",
        string barcode = "1234567890",
        string catalogNumber = "CAT-001",
        string formatCode = "DIGI") => new()
    {
        Isrc = isrc,
        Barcode = barcode,
        CatalogNumber = catalogNumber,
        ProductFormatCode = formatCode,
        ProductName = "Test Product",
        Artist = "Test Artist",
        ProductTypeId = 1,
        CreateTime = DateTime.UtcNow,
        ArchiveLevel = 0
    };

    private CatalogProductRights MakeRights(int productId,
        string docNumber = "AGR-001",
        string territory = "RU",
        string sender = "Sender LLC",
        string receiver = "Recipient LLC") => new()
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

    // ──────────────── Тесты: статус 0 (нет продукта) ─────────────────────────

    [Fact]
    public async Task ValidateSingle_EmptyDatabase_Returns_NoProduct()
    {
        var result = await _service.ValidateSingleAsync(MakeDto());

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
        result.ProductId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateSingle_ProductNotFound_DueToIsrcMismatch_Returns_NoProduct()
    {
        _db.CatalogProducts.Add(MakeProduct(isrc: "ISRC999"));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto(isrc: "ISRC001"));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
    }

    [Fact]
    public async Task ValidateSingle_ProductNotFound_DueToBarcodeMismatch_Returns_NoProduct()
    {
        _db.CatalogProducts.Add(MakeProduct(barcode: "0000000000"));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto(barcode: "1234567890"));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
    }

    [Fact]
    public async Task ValidateSingle_ProductNotFound_DueToFormatCodeMismatch_Returns_NoProduct()
    {
        _db.CatalogProducts.Add(MakeProduct(formatCode: "CD"));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto(productFormatCode: "DIGI"));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
    }

    [Fact]
    public async Task ValidateSingle_ArchivedProduct_Returns_NoProduct()
    {
        var p = MakeProduct();
        p.ArchiveLevel = 1; // мягкое удаление
        _db.CatalogProducts.Add(p);
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto());

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
    }

    [Theory]
    [InlineData("", "1234567890", "CAT-001", "DIGI")]  // Пустой ISRC
    [InlineData("ISRC001", "", "CAT-001", "DIGI")]     // Пустой Barcode
    [InlineData("ISRC001", "1234567890", "", "DIGI")]  // Пустой CatalogNumber
    [InlineData("ISRC001", "1234567890", "CAT-001", "")] // Пустой ProductFormatCode
    public async Task ValidateSingle_MissingIdentifierField_Returns_NoProduct(
        string isrc, string barcode, string catalogNumber, string formatCode)
    {
        _db.CatalogProducts.Add(MakeProduct());
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(
            MakeDto(isrc, barcode, catalogNumber, formatCode));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
    }

    // ──────────────── Тесты: статус 1 (нет прав) ────────────────────────────

    [Fact]
    public async Task ValidateSingle_ProductFound_NoRights_Returns_NoRights()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto());

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
        result.ProductId.Should().Be(p.Id);
    }

    [Fact]
    public async Task ValidateSingle_MissingAgreementNumber_Returns_NoRights()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        _db.CatalogProductRights.Add(MakeRights(p.Id));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto(agreementNumber: ""));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
    }

    [Fact]
    public async Task ValidateSingle_MissingTerritoryCode_Returns_NoRights()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        _db.CatalogProductRights.Add(MakeRights(p.Id));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto(territoryCode: ""));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
    }

    [Fact]
    public async Task ValidateSingle_EmptySenderAndReceiver_Returns_NoRights()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        _db.CatalogProductRights.Add(MakeRights(p.Id));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(
            MakeDto(senderCompany: "", recipientCompany: ""));

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
    }

    [Fact]
    public async Task ValidateSingle_ArchivedRights_Returns_NoRights()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        var rights = MakeRights(p.Id);
        rights.ArchiveLevel = 1;
        _db.CatalogProductRights.Add(rights);
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto());

        result.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
    }

    // ──────────────── Тесты: статус 88 (валидировано) ───────────────────────

    [Fact]
    public async Task ValidateSingle_ProductAndRightsFound_Returns_Validated()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        _db.CatalogProductRights.Add(MakeRights(p.Id));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto());

        result.BusinessStatus.Should().Be((int)BusinessStatus.Validated);
        result.ProductId.Should().Be(p.Id);
    }

    [Fact]
    public async Task ValidateSingle_MatchByCompanyId_Returns_Validated()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        await _db.SaveChangesAsync();

        var rights = MakeRights(p.Id);
        rights.CompanySenderId = 5;
        rights.CompanyReceiverId = 7;
        _db.CatalogProductRights.Add(rights);
        await _db.SaveChangesAsync();

        var dto = MakeDto();
        dto.SenderCompanyId = 5;
        dto.RecipientCompanyId = 7;
        dto.SenderCompany = null;
        dto.RecipientCompany = null;

        var result = await _service.ValidateSingleAsync(dto);

        result.BusinessStatus.Should().Be((int)BusinessStatus.Validated);
    }

    // ──────────────── Тесты: пакетная валидация ───────────────────────────────

    [Fact]
    public async Task ValidateBatch_EmptyList_ReturnsZeroTotals()
    {
        var result = await _service.ValidateBatchAsync([]);

        result.TotalRows.Should().Be(0);
        result.NoProductCount.Should().Be(0);
        result.NoRightsCount.Should().Be(0);
        result.ValidatedCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidateBatch_ThreeLines_AllNoProduct_CorrectCounts()
    {
        var lines = new List<SuspenseLineDto>
        {
            MakeDto("ISRC001"), MakeDto("ISRC002"), MakeDto("ISRC003")
        };

        var result = await _service.ValidateBatchAsync(lines);

        result.TotalRows.Should().Be(3);
        result.NoProductCount.Should().Be(3);
        result.NoRightsCount.Should().Be(0);
        result.ValidatedCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidateBatch_MixedResults_CorrectCounts()
    {
        // Продукт 1: есть, права есть → статус 88
        var p1 = MakeProduct("ISRC001", "BC001", "CAT001");
        _db.CatalogProducts.Add(p1);
        await _db.SaveChangesAsync();
        _db.CatalogProductRights.Add(MakeRights(p1.Id));

        // Продукт 2: есть, прав нет → статус 1
        var p2 = MakeProduct("ISRC002", "BC002", "CAT002");
        _db.CatalogProducts.Add(p2);
        await _db.SaveChangesAsync();

        var lines = new List<SuspenseLineDto>
        {
            MakeDto("ISRC001", "BC001", "CAT001"),  // → 88
            MakeDto("ISRC002", "BC002", "CAT002"),  // → 1
            MakeDto("ISRC999", "BC999", "CAT999"),  // → 0
        };

        var result = await _service.ValidateBatchAsync(lines);

        result.TotalRows.Should().Be(3);
        result.ValidatedCount.Should().Be(1);
        result.NoRightsCount.Should().Be(1);
        result.NoProductCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateBatch_SavesAllLinesToDatabase()
    {
        var lines = new List<SuspenseLineDto>
        {
            MakeDto("ISRC001"), MakeDto("ISRC002")
        };

        await _service.ValidateBatchAsync(lines);

        var count = await _db.SuspenseLines.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task ValidateSingle_CreatesEntityWithCorrectFields()
    {
        var dto = MakeDto();

        await _service.ValidateSingleAsync(dto);

        var entity = await _db.SuspenseLines.FirstAsync();
        entity.Isrc.Should().Be(dto.Isrc);
        entity.Artist.Should().Be(dto.Artist);
        entity.ArchiveLevel.Should().Be(0);
        entity.CreateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ValidateSingle_LineCountInDb_IsOne()
    {
        await _service.ValidateSingleAsync(MakeDto());

        (await _db.SuspenseLines.CountAsync()).Should().Be(1);
    }

    // ──────────────── Корректность CauseSuspense ─────────────────────────────

    [Fact]
    public async Task ValidateSingle_NoProduct_CauseSuspenseIsSet()
    {
        var result = await _service.ValidateSingleAsync(MakeDto());
        result.CauseSuspense.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidateSingle_Validated_CauseSuspenseIsSet()
    {
        var p = MakeProduct();
        _db.CatalogProducts.Add(p);
        _db.CatalogProductRights.Add(MakeRights(p.Id));
        await _db.SaveChangesAsync();

        var result = await _service.ValidateSingleAsync(MakeDto());
        result.CauseSuspense.Should().NotBeNullOrWhiteSpace();
    }
}
