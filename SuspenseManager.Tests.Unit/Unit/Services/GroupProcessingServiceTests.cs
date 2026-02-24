using Application.Services;
using Common.DTOs;
using Common.Exceptions;
using Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Unit.Unit.Services;

/// <summary>
/// Модульные тесты GroupProcessingService.
/// Проверяют корректность переходов статусов, бизнес-правил и поведение при ошибках.
/// </summary>
public class GroupProcessingServiceTests : IDisposable
{
    private readonly SuspenseManagerDbContext _db;
    private readonly GroupProcessingService _service;

    public GroupProcessingServiceTests()
    {
        var options = new DbContextOptionsBuilder<SuspenseManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SuspenseManagerDbContext(options);
        _service = new GroupProcessingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ──────────────────────── Helpers ────────────────────────────────────────

    private async Task<SuspenseGroup> CreateGroupAsync(
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
            ArchiveLevel = 0
        };
        _db.SuspenseGroups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    private async Task<SuspenseLine> CreateSuspenseInGroupAsync(
        int groupId, int status = (int)BusinessStatus.InGroupNoProduct, int? productId = null)
    {
        var s = new SuspenseLine
        {
            GroupId = groupId,
            BusinessStatus = status,
            ProductId = productId,
            Isrc = "ISRC001",
            Artist = "Artist",
            Qty = 100,
            ExchangeCurrency = 1m,
            ExchangeRate = 1m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.SuspenseLines.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    private async Task<CatalogProduct> CreateProductAsync(int productTypeId = 1)
    {
        var p = new CatalogProduct
        {
            Isrc = "ISRC001",
            Barcode = "1234567890",
            CatalogNumber = "CAT-001",
            ProductFormatCode = "DIGI",
            ProductName = "Test Product",
            Artist = "Test Artist",
            ProductTypeId = productTypeId,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.CatalogProducts.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    private async Task<CatalogProductType> CreateProductTypeAsync(string code = "DIGI")
    {
        var t = new CatalogProductType
        {
            Code = code,
            Description = "Digital",
            CreateTime = DateTime.UtcNow
        };
        _db.CatalogProductTypes.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    // ──────────────── PostponeAsync ───────────────────────────────────────────

    [Fact]
    public async Task PostponeAsync_Status15_TransitionsTo_30()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.PostponeAsync(group.Id, new PostponeGroupDto());

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.PostponedNoProduct);
    }

    [Fact]
    public async Task PostponeAsync_Status16_TransitionsTo_32()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoRights);

        await _service.PostponeAsync(group.Id, new PostponeGroupDto());

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.PostponedNoRights);
    }

    [Fact]
    public async Task PostponeAsync_SuspensesAlsoUpdateStatus()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.PostponeAsync(group.Id, new PostponeGroupDto());

        var suspense = await _db.SuspenseLines.FirstAsync(s => s.GroupId == group.Id);
        suspense.BusinessStatus.Should().Be((int)BusinessStatus.PostponedNoProduct);
    }

    [Fact]
    public async Task PostponeAsync_InvalidStatus_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.PostponedNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.PostponeAsync(group.Id, new PostponeGroupDto()));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public async Task PostponeAsync_GroupNotFound_Throws_KeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.PostponeAsync(99999, new PostponeGroupDto()));
    }

    // ──────────────── ReturnFromPostponedAsync ────────────────────────────────

    [Fact]
    public async Task ReturnFromPostponed_Status30_TransitionsTo_15()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.PostponedNoProduct);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.PostponedNoProduct);

        await _service.ReturnFromPostponedAsync(group.Id);

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoProduct);
    }

    [Fact]
    public async Task ReturnFromPostponed_Status32_TransitionsTo_16()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.PostponedNoRights);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.PostponedNoRights);

        await _service.ReturnFromPostponedAsync(group.Id);

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
    }

    [Fact]
    public async Task ReturnFromPostponed_NotPostponedGroup_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.ReturnFromPostponedAsync(group.Id));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── SendToBackOfficeAsync ──────────────────────────────────

    [Fact]
    public async Task SendToBackOffice_Status15_TransitionsTo_120()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.SendToBackOfficeAsync(group.Id, new SendToBackOfficeDto());

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.BackOfficeNoProduct);
    }

    [Fact]
    public async Task SendToBackOffice_Status16_TransitionsTo_320()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoRights);

        await _service.SendToBackOfficeAsync(group.Id, new SendToBackOfficeDto());

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.BackOfficeNoRights);
    }

    [Fact]
    public async Task SendToBackOffice_InvalidStatus_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.PostponedNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.SendToBackOfficeAsync(group.Id, new SendToBackOfficeDto()));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── UngroupAsync ────────────────────────────────────────────

    [Fact]
    public async Task Ungroup_Status15_ArchivesGroup_And_RevertsSuspensesTo_0()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        var s1 = await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);
        var s2 = await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.UngroupAsync(group.Id);

        var updatedGroup = await _db.SuspenseGroups.FindAsync(group.Id);
        updatedGroup!.ArchiveLevel.Should().BeGreaterThan(0, "группа должна быть архивирована");
        Assert.NotNull(updatedGroup.ArchiveTime);

        var suspenses = await _db.SuspenseLines
            .Where(s => s.Id == s1.Id || s.Id == s2.Id)
            .ToListAsync();
        suspenses.Should().AllSatisfy(s =>
        {
            s.BusinessStatus.Should().Be((int)BusinessStatus.NoProduct);
            s.GroupId.Should().BeNull("суспенсы должны быть исключены из группы");
        });
    }

    [Fact]
    public async Task Ungroup_Status16_RevertsSuspensesTo_1_And_PreservesProductId()
    {
        // Бизнес-правило 5: при разгруппировке 16 ProductId СОХРАНЯЕТСЯ
        var product = await CreateProductAsync();
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights, productId: product.Id);
        var s = await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoRights, product.Id);

        await _service.UngroupAsync(group.Id);

        var updatedSuspense = await _db.SuspenseLines.FindAsync(s.Id);
        updatedSuspense!.BusinessStatus.Should().Be((int)BusinessStatus.NoRights);
        updatedSuspense.GroupId.Should().BeNull();
        updatedSuspense.ProductId.Should().Be(product.Id,
            "ProductId должен сохраниться при разгруппировке — бизнес-правило 5");
    }

    [Fact]
    public async Task Ungroup_InvalidStatus_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.PostponedNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.UngroupAsync(group.Id));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── LinkProductAsync ────────────────────────────────────────

    [Fact]
    public async Task LinkProduct_Status15_TransitionsGroupTo_16()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        var product = await CreateProductAsync();
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.LinkProductAsync(group.Id, new LinkProductDto { ProductId = product.Id });

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
        updated.CatalogProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task LinkProduct_SuspensesTransitionTo_16()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        var product = await CreateProductAsync();
        var s = await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.LinkProductAsync(group.Id, new LinkProductDto { ProductId = product.Id });

        var updated = await _db.SuspenseLines.FindAsync(s.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
        updated.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task LinkProduct_ProductNotFound_Throws_PRODUCT_NOT_FOUND()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LinkProductAsync(group.Id, new LinkProductDto { ProductId = 99999 }));

        ex.BusinessCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task LinkProduct_ArchivedProduct_Throws_PRODUCT_NOT_FOUND()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        var product = await CreateProductAsync();
        product.ArchiveLevel = 1;
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LinkProductAsync(group.Id, new LinkProductDto { ProductId = product.Id }));

        ex.BusinessCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task LinkProduct_InvalidStatus_Throws_INVALID_STATUS()
    {
        var product = await CreateProductAsync();
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LinkProductAsync(group.Id, new LinkProductDto { ProductId = product.Id }));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── QuickCatalogAsync ───────────────────────────────────────

    [Fact]
    public async Task QuickCatalog_Status15_CreatesProduct_And_TransitionsTo_16()
    {
        var productType = await CreateProductTypeAsync("DIGI");
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        var product = await _service.QuickCatalogAsync(group.Id);

        product.Should().NotBeNull();
        (await _db.CatalogProducts.FindAsync(product.Id)).Should().NotBeNull();

        var updatedGroup = await _db.SuspenseGroups.FindAsync(group.Id);
        updatedGroup!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
        updatedGroup.CatalogProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task QuickCatalog_InvalidStatus16_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.QuickCatalogAsync(group.Id));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── UpdateMetadataAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateMetadata_CreatesMetadata_WhenNoneExists()
    {
        var group = await CreateGroupAsync();

        await _service.UpdateMetadataAsync(group.Id, new UpdateGroupMetadataDto
        {
            Title = "Test Title",
            Artist = "Test Artist"
        });

        var meta = await _db.GroupMetadata.FirstOrDefaultAsync(m => m.SuspenseGroupId == group.Id);
        meta.Should().NotBeNull();
        meta!.Title.Should().Be("Test Title");
        meta.Artist.Should().Be("Test Artist");
    }

    [Fact]
    public async Task UpdateMetadata_UpdatesExistingMetadata()
    {
        var group = await CreateGroupAsync();
        await _service.UpdateMetadataAsync(group.Id, new UpdateGroupMetadataDto { Title = "Old Title" });

        await _service.UpdateMetadataAsync(group.Id, new UpdateGroupMetadataDto { Title = "New Title" });

        var meta = await _db.GroupMetadata.FirstAsync(m => m.SuspenseGroupId == group.Id);
        meta.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateMetadata_SetsCatalogProductId_TriggersStatus15To16()
    {
        // Бизнес-правило: установка CatalogProductId в метаданных переводит группу 15→16
        var product = await CreateProductAsync();
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);
        var s = await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoProduct);

        await _service.UpdateMetadataAsync(group.Id, new UpdateGroupMetadataDto
        {
            CatalogProductId = product.Id
        });

        var updatedGroup = await _db.SuspenseGroups.FindAsync(group.Id);
        updatedGroup!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights,
            "установка CatalogProductId — триггер перехода 15→16");
        updatedGroup.CatalogProductId.Should().Be(product.Id);

        var updatedSuspense = await _db.SuspenseLines.FindAsync(s.Id);
        updatedSuspense!.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
    }

    [Fact]
    public async Task UpdateMetadata_SetInvalidProductId_Throws_PRODUCT_NOT_FOUND()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.UpdateMetadataAsync(group.Id, new UpdateGroupMetadataDto
            {
                CatalogProductId = 99999
            }));

        ex.BusinessCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    // ──────────────── UpdateMetaRightsAsync ──────────────────────────────────

    [Fact]
    public async Task UpdateMetaRights_Status16_CreatesMetaRights()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);

        await _service.UpdateMetaRightsAsync(group.Id, new UpdateGroupMetaRightsDto
        {
            DocNumber = "DOC-001",
            TerritoryCode = "RU"
        });

        var metaRights = await _db.GroupMetaRights.FirstOrDefaultAsync(m => m.GroupId == group.Id);
        metaRights.Should().NotBeNull();
        metaRights!.DocNumber.Should().Be("DOC-001");
    }

    [Fact]
    public async Task UpdateMetaRights_Status15_Throws_INVALID_STATUS()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoProduct);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.UpdateMetaRightsAsync(group.Id, new UpdateGroupMetaRightsDto()));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    // ──────────────── ValidateGroupAsync ─────────────────────────────────────

    [Fact]
    public async Task ValidateGroup_TransitionsTo_88()
    {
        var group = await CreateGroupAsync((int)BusinessStatus.InGroupNoRights);
        await CreateSuspenseInGroupAsync(group.Id, (int)BusinessStatus.InGroupNoRights);

        await _service.ValidateGroupAsync(group.Id);

        var updated = await _db.SuspenseGroups.FindAsync(group.Id);
        updated!.BusinessStatus.Should().Be((int)BusinessStatus.Validated);

        var suspense = await _db.SuspenseLines.FirstAsync(s => s.GroupId == group.Id);
        suspense.BusinessStatus.Should().Be((int)BusinessStatus.Validated);
    }
}
