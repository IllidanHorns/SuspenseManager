using Application.Services;
using Common.DTOs;
using Common.Exceptions;
using Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Models;
using Models.Enums;

namespace SuspenseManager.Tests.Unit.Unit.Services;

/// <summary>
/// Тесты GroupingService.CommitAsync — фиксация группы через EF Core LINQ.
/// PreviewAsync тестируется в интеграционных тестах (требует SQL Server).
/// </summary>
public class GroupingCommitTests : IDisposable
{
    private readonly SuspenseManagerDbContext _db;
    private readonly GroupingService _service;

    public GroupingCommitTests()
    {
        var options = new DbContextOptionsBuilder<SuspenseManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new SuspenseManagerDbContext(options);
        _service = new GroupingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ──────────────────────── Helpers ────────────────────────────────────────

    private async Task<SuspenseLine> AddNoProductSuspenseAsync(
        string artist = "Artist1",
        string territory = "RU")
    {
        var s = new SuspenseLine
        {
            BusinessStatus = (int)BusinessStatus.NoProduct,
            Artist = artist,
            TerritoryCode = territory,
            Isrc = "ISRC001",
            Qty = 100,
            ExchangeCurrency = 1m,
            ExchangeRate = 1m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
            GroupId = null
        };
        _db.SuspenseLines.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    private async Task<(SuspenseLine suspense, CatalogProduct product)> AddNoRightsSuspenseAsync(
        string artist = "Artist1")
    {
        var product = new CatalogProduct
        {
            Isrc = "ISRC001",
            Barcode = "BC001",
            CatalogNumber = "CAT001",
            ProductFormatCode = "DIGI",
            ProductName = "Album",
            Artist = artist,
            ProductTypeId = 1,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.CatalogProducts.Add(product);
        await _db.SaveChangesAsync();

        var s = new SuspenseLine
        {
            BusinessStatus = (int)BusinessStatus.NoRights,
            ProductId = product.Id,
            Artist = artist,
            TerritoryCode = "RU",
            Isrc = "ISRC001",
            Qty = 100,
            ExchangeCurrency = 1m,
            ExchangeRate = 1m,
            CauseSuspense = "",
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
            GroupId = null
        };
        _db.SuspenseLines.Add(s);
        await _db.SaveChangesAsync();
        return (s, product);
    }

    // ──────────────── CommitAsync — статус 0 ─────────────────────────────────

    [Fact]
    public async Task Commit_Status0_MatchingLine_CreatesGroup()
    {
        var suspense = await AddNoProductSuspenseAsync("Beatles", "RU");

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist", "TerritoryCode"],
            KeyValues = new Dictionary<string, string?>
            {
                ["Artist"] = "Beatles",
                ["TerritoryCode"] = "RU"
            },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        group.Should().NotBeNull();
        group.Id.Should().BeGreaterThan(0);
        group.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoProduct);
    }

    [Fact]
    public async Task Commit_Status0_SuspenseLinkUpdated()
    {
        var suspense = await AddNoProductSuspenseAsync("Beatles", "RU");

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist"],
            KeyValues = new Dictionary<string, string?> { ["Artist"] = "Beatles" },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        var updated = await _db.SuspenseLines.FindAsync(suspense.Id);
        updated!.GroupId.Should().Be(group.Id);
        updated.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoProduct);
    }

    [Fact]
    public async Task Commit_Status0_SuspenseGroupLinkCreated()
    {
        var suspense = await AddNoProductSuspenseAsync("Beatles", "RU");

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist"],
            KeyValues = new Dictionary<string, string?> { ["Artist"] = "Beatles" },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        var links = await _db.SuspenseGroupLinks
            .Where(l => l.SuspenseGroupId == group.Id)
            .ToListAsync();

        links.Should().HaveCount(1);
        links[0].SuspenseId.Should().Be(suspense.Id);
    }

    [Fact]
    public async Task Commit_Status0_OnlyMatchingLinesGrouped()
    {
        await AddNoProductSuspenseAsync("Beatles", "RU");
        await AddNoProductSuspenseAsync("Rolling Stones", "US"); // не должна попасть

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist"],
            KeyValues = new Dictionary<string, string?> { ["Artist"] = "Beatles" },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        var links = await _db.SuspenseGroupLinks
            .Where(l => l.SuspenseGroupId == group.Id)
            .ToListAsync();

        links.Should().HaveCount(1, "только Beatles должны войти в группу");
    }

    [Fact]
    public async Task Commit_Status0_AlreadyGroupedLine_NotIncluded()
    {
        // Добавляем суспенс уже в группе
        var existingGroup = new SuspenseGroup
        {
            BusinessStatus = (int)BusinessStatus.InGroupNoProduct,
            AccountId = 1,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.SuspenseGroups.Add(existingGroup);
        await _db.SaveChangesAsync();

        var alreadyGrouped = await AddNoProductSuspenseAsync("Beatles", "RU");
        alreadyGrouped.GroupId = existingGroup.Id;
        await _db.SaveChangesAsync();

        // Добавляем свободный суспенс с тем же артистом
        await AddNoProductSuspenseAsync("Beatles", "RU");

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist"],
            KeyValues = new Dictionary<string, string?> { ["Artist"] = "Beatles" },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        var links = await _db.SuspenseGroupLinks
            .Where(l => l.SuspenseGroupId == group.Id)
            .ToListAsync();

        links.Should().HaveCount(1, "уже сгруппированные суспенсы не должны попасть в новую группу");
    }

    [Fact]
    public async Task Commit_Status0_NoMatchingSuspenses_Throws_NO_MATCHING_SUSPENSES()
    {
        var request = new GroupingCommitRequest
        {
            BusinessStatus = 0,
            GroupByColumns = ["Artist"],
            KeyValues = new Dictionary<string, string?> { ["Artist"] = "NonExistent" },
            AccountId = 1
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CommitAsync(request));
        ex.BusinessCode.Should().Be("NO_MATCHING_SUSPENSES");
    }

    // ──────────────── CommitAsync — статус 1 ─────────────────────────────────

    [Fact]
    public async Task Commit_Status1_MatchingLine_CreatesGroupWithCorrectStatus()
    {
        var (suspense, product) = await AddNoRightsSuspenseAsync("Beatles");

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 1,
            GroupByColumns = ["ProductId"],
            KeyValues = new Dictionary<string, string?> { ["ProductId"] = product.Id.ToString() },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        group.BusinessStatus.Should().Be((int)BusinessStatus.InGroupNoRights);
        group.CatalogProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Commit_Status1_GroupLinkedToCatalogProduct()
    {
        var (_, product) = await AddNoRightsSuspenseAsync();

        var request = new GroupingCommitRequest
        {
            BusinessStatus = 1,
            GroupByColumns = ["ProductId"],
            KeyValues = new Dictionary<string, string?> { ["ProductId"] = product.Id.ToString() },
            AccountId = 1
        };

        var group = await _service.CommitAsync(request);

        group.CatalogProductId.Should().Be(product.Id,
            "группа статуса 1 должна быть связана с продуктом из каталога");
    }

    // ──────────────── Валидация запроса ──────────────────────────────────────

    [Fact]
    public async Task Commit_InvalidStatus_Throws_INVALID_STATUS()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CommitAsync(new GroupingCommitRequest
            {
                BusinessStatus = 99,
                GroupByColumns = ["Isrc"],
                KeyValues = new Dictionary<string, string?> { ["Isrc"] = "X" },
                AccountId = 1
            }));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public async Task Commit_Status1_WithoutProductId_Throws_PRODUCT_ID_REQUIRED()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CommitAsync(new GroupingCommitRequest
            {
                BusinessStatus = 1,
                GroupByColumns = ["Artist"],
                KeyValues = new Dictionary<string, string?> { ["Artist"] = "X" },
                AccountId = 1
            }));

        ex.BusinessCode.Should().Be("PRODUCT_ID_REQUIRED");
    }
}
