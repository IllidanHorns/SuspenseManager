using Application.Helpers;
using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class CatalogService : ICatalogService
{
    private readonly SuspenseManagerDbContext _db;

    public CatalogService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    // ── Типы продуктов ──────────────────────────────────────────────────────────

    public async Task<List<CatalogProductType>> GetProductTypesAsync(CancellationToken ct = default)
    {
        return await _db.CatalogProductTypes
            .AsNoTracking()
            .Where(t => t.ArchiveLevel == 0)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
    }

    // ── Продукты ────────────────────────────────────────────────────────────────

    public async Task<PagedResponse<CatalogProduct>> GetProductsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var identityIncompleteOnly = false;
        Dictionary<string, string>? filtersForPaging = null;
        if (request.Filters != null && request.Filters.Count > 0)
        {
            filtersForPaging = new Dictionary<string, string>(request.Filters);
            if (filtersForPaging.TryGetValue("IdentityIncomplete", out var inc) &&
                string.Equals(inc, "true", StringComparison.OrdinalIgnoreCase))
            {
                identityIncompleteOnly = true;
                filtersForPaging.Remove("IdentityIncomplete");
            }

            if (filtersForPaging.Count == 0)
                filtersForPaging = null;
        }

        var effectiveRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Filters = filtersForPaging
        };

        var query = _db.CatalogProducts
            .AsNoTracking()
            .Where(p => p.ArchiveLevel == 0);

        if (identityIncompleteOnly)
            query = query.Where(CatalogProductIdentity.IncompletePredicate);

        query = query.OrderByDescending(p => p.CreateTime);

        return await query.ToPagedResponseAsync(effectiveRequest, ct);
    }

    public async Task<CatalogProduct> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.CatalogProducts
            .AsNoTracking()
            .Include(p => p.ProductType)
            .Include(p => p.ProductRights.Where(r => r.ArchiveLevel == 0))
                .ThenInclude(r => r.CompanySenderR)
            .Include(p => p.ProductRights.Where(r => r.ArchiveLevel == 0))
                .ThenInclude(r => r.CompanyReceiverR)
            .Include(p => p.ProductRights.Where(r => r.ArchiveLevel == 0))
                .ThenInclude(r => r.Territory)
            .FirstOrDefaultAsync(p => p.Id == id && p.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Продукт #{id} не найден");
    }

    public async Task<CatalogProduct> CreateProductAsync(CreateCatalogProductDto dto, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.Isrc) &&
            await _db.CatalogProducts.AnyAsync(p => p.Isrc == dto.Isrc && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с ISRC «{dto.Isrc}» уже существует в каталоге", "PRODUCT_ISRC_EXISTS", 409);

        if (!string.IsNullOrWhiteSpace(dto.Barcode) &&
            await _db.CatalogProducts.AnyAsync(p => p.Barcode == dto.Barcode && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с баркодом «{dto.Barcode}» уже существует в каталоге", "PRODUCT_BARCODE_EXISTS", 409);

        if (!string.IsNullOrWhiteSpace(dto.CatalogNumber) &&
            await _db.CatalogProducts.AnyAsync(p => p.CatalogNumber == dto.CatalogNumber && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с каталожным номером «{dto.CatalogNumber}» уже существует в каталоге", "PRODUCT_CATALOG_NUMBER_EXISTS", 409);

        int productTypeId = dto.ProductTypeId ?? await GetDefaultProductTypeIdAsync(dto.ProductFormatCode, ct);

        var product = new CatalogProduct
        {
            ProductName      = dto.ProductName,
            Artist           = dto.Artist,
            Isrc             = dto.Isrc ?? string.Empty,
            Barcode          = dto.Barcode ?? string.Empty,
            CatalogNumber    = dto.CatalogNumber ?? string.Empty,
            ProductFormatCode = dto.ProductFormatCode ?? "DIGI",
            Genre            = dto.Genre,
            AlbumName        = dto.AlbumName,
            Composer         = dto.Composer,
            Description      = dto.Description,
            ReleaseDate      = dto.ReleaseDate,
            ProductTypeId    = productTypeId,
            CreateTime       = DateTime.UtcNow,
            ArchiveLevel     = 0,
        };

        _db.CatalogProducts.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<CatalogProduct> UpdateProductAsync(int id, UpdateCatalogProductDto dto, CancellationToken ct = default)
    {
        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == id && p.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Продукт #{id} не найден");

        if (dto.Isrc != null && !string.IsNullOrWhiteSpace(dto.Isrc) &&
            await _db.CatalogProducts.AnyAsync(p => p.Isrc == dto.Isrc && p.Id != id && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с ISRC «{dto.Isrc}» уже существует в каталоге", "PRODUCT_ISRC_EXISTS", 409);

        if (dto.Barcode != null && !string.IsNullOrWhiteSpace(dto.Barcode) &&
            await _db.CatalogProducts.AnyAsync(p => p.Barcode == dto.Barcode && p.Id != id && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с баркодом «{dto.Barcode}» уже существует в каталоге", "PRODUCT_BARCODE_EXISTS", 409);

        if (dto.CatalogNumber != null && !string.IsNullOrWhiteSpace(dto.CatalogNumber) &&
            await _db.CatalogProducts.AnyAsync(p => p.CatalogNumber == dto.CatalogNumber && p.Id != id && p.ArchiveLevel == 0, ct))
            throw new BusinessException($"Продукт с каталожным номером «{dto.CatalogNumber}» уже существует в каталоге", "PRODUCT_CATALOG_NUMBER_EXISTS", 409);

        if (dto.ProductName    != null) product.ProductName    = dto.ProductName;
        if (dto.Artist         != null) product.Artist         = dto.Artist;
        if (dto.Isrc           != null) product.Isrc           = dto.Isrc;
        if (dto.Barcode        != null) product.Barcode        = dto.Barcode;
        if (dto.CatalogNumber  != null) product.CatalogNumber  = dto.CatalogNumber;
        if (dto.Genre          != null) product.Genre          = dto.Genre;
        if (dto.AlbumName      != null) product.AlbumName      = dto.AlbumName;
        if (dto.Composer       != null) product.Composer       = dto.Composer;
        if (dto.Description    != null) product.Description    = dto.Description;
        if (dto.ReleaseDate    != null) product.ReleaseDate    = dto.ReleaseDate;
        if (dto.ProductTypeId  != null) product.ProductTypeId  = dto.ProductTypeId.Value;
        if (dto.ProductFormatCode != null) product.ProductFormatCode = dto.ProductFormatCode;

        CatalogProductIdentity.EnsureCompleteProductOrThrow(product);

        product.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return product;
    }

    // ── Права на продукт ────────────────────────────────────────────────────────

    public async Task<PagedResponse<CatalogProductRights>> GetRightsAsync(
        PagedRequest request, int? productId, CancellationToken ct = default)
    {
        var rightsIncompleteOnly = false;
        Dictionary<string, string>? filtersForPaging = null;
        if (request.Filters != null && request.Filters.Count > 0)
        {
            filtersForPaging = new Dictionary<string, string>(request.Filters);
            if (filtersForPaging.TryGetValue("RightsIncomplete", out var ri) &&
                string.Equals(ri, "true", StringComparison.OrdinalIgnoreCase))
            {
                rightsIncompleteOnly = true;
                filtersForPaging.Remove("RightsIncomplete");
            }

            if (filtersForPaging.Count == 0)
                filtersForPaging = null;
        }

        var effectiveRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Filters = filtersForPaging
        };

        var query = _db.CatalogProductRights
            .AsNoTracking()
            .Include(r => r.CompanySenderR)
            .Include(r => r.CompanyReceiverR)
            .Include(r => r.Territory)
            .Include(r => r.CatalogProduct)
            .Where(r => r.ArchiveLevel == 0);

        if (productId.HasValue)
            query = query.Where(r => r.CatalogProductId == productId.Value);

        if (rightsIncompleteOnly)
            query = query.Where(CatalogProductRightsIdentity.IncompletePredicate);

        return await query.ToPagedResponseAsync(effectiveRequest, ct);
    }

    public async Task<CatalogProductRights> CreateRightsAsync(
        CreateCatalogProductRightsDto dto, CancellationToken ct = default)
    {
        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == dto.CatalogProductId && p.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

        var sender = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == dto.CompanySenderId && c.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Компания-отправитель не найдена", "COMPANY_NOT_FOUND", 404);

        var receiver = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == dto.CompanyReceiverId && c.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Компания-получатель не найдена", "COMPANY_NOT_FOUND", 404);

        var territory = await _db.Territories
            .FirstOrDefaultAsync(t => t.Id == dto.TerritoryId && t.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Территория не найдена", "TERRITORY_NOT_FOUND", 404);

        CatalogProductRightsIdentity.EnsureCompleteForCreate(dto, territory);

        var duplicate = await _db.CatalogProductRights.AnyAsync(r =>
            r.CatalogProductId == dto.CatalogProductId &&
            r.CompanySenderId  == dto.CompanySenderId &&
            r.TerritoryCode    == territory.TerritoryCode &&
            r.DocNumber        == dto.DocNumber &&
            r.ArchiveLevel     == 0, ct);
        if (duplicate)
            throw new BusinessException(
                "Запись прав с такой комбинацией продукта, отправителя, территории и номера договора уже существует",
                "RIGHTS_DUPLICATE", 409);

        var rights = new CatalogProductRights
        {
            CatalogProductId  = product.Id,
            DocNumber         = dto.DocNumber,
            CompanySenderId   = sender.Id,
            CompanySender     = sender.ShortName,
            CompanyReceiverId = receiver.Id,
            CompanyReceiver   = receiver.ShortName,
            Share             = dto.Share,
            TerritoryId       = territory.Id,
            TerritoryCode     = territory.TerritoryCode,
            TerritoryDesc     = territory.TerritoryName ?? string.Empty,
            DocStart          = dto.DocStart,
            DocEnd            = dto.DocEnd,
            CreateTime        = DateTime.UtcNow,
            ArchiveLevel      = 0,
        };

        _db.CatalogProductRights.Add(rights);
        await _db.SaveChangesAsync(ct);
        return rights;
    }

    public async Task<CatalogProductRights> UpdateRightsAsync(
        int id, UpdateCatalogProductRightsDto dto, CancellationToken ct = default)
    {
        var rights = await _db.CatalogProductRights
            .FirstOrDefaultAsync(r => r.Id == id && r.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Права #{id} не найдены");

        if (dto.CompanySenderId.HasValue)
        {
            var sender = await _db.Companies
                .FirstOrDefaultAsync(c => c.Id == dto.CompanySenderId.Value && c.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Компания-отправитель не найдена", "COMPANY_NOT_FOUND", 404);
            rights.CompanySenderId = sender.Id;
            rights.CompanySender   = sender.ShortName;
        }

        if (dto.CompanyReceiverId.HasValue)
        {
            var receiver = await _db.Companies
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyReceiverId.Value && c.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Компания-получатель не найдена", "COMPANY_NOT_FOUND", 404);
            rights.CompanyReceiverId = receiver.Id;
            rights.CompanyReceiver   = receiver.ShortName;
        }

        if (dto.TerritoryId.HasValue)
        {
            var territory = await _db.Territories
                .FirstOrDefaultAsync(t => t.Id == dto.TerritoryId.Value && t.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Территория не найдена", "TERRITORY_NOT_FOUND", 404);
            rights.TerritoryId   = territory.Id;
            rights.TerritoryCode = territory.TerritoryCode;
            rights.TerritoryDesc = territory.TerritoryName ?? string.Empty;
        }

        if (dto.DocNumber != null) rights.DocNumber = dto.DocNumber;
        if (dto.Share     != null) rights.Share     = dto.Share.Value;
        if (dto.DocStart  != null) rights.DocStart  = dto.DocStart.Value;
        if (dto.DocEnd    != null) rights.DocEnd    = dto.DocEnd.Value;

        var duplicate = await _db.CatalogProductRights.AnyAsync(r =>
            r.CatalogProductId == rights.CatalogProductId &&
            r.CompanySenderId  == rights.CompanySenderId &&
            r.TerritoryCode    == rights.TerritoryCode &&
            r.DocNumber        == rights.DocNumber &&
            r.Id               != id &&
            r.ArchiveLevel     == 0, ct);
        if (duplicate)
            throw new BusinessException(
                "Запись прав с такой комбинацией продукта, отправителя, территории и номера договора уже существует",
                "RIGHTS_DUPLICATE", 409);

        CatalogProductRightsIdentity.EnsureCompleteRightsRecordOrThrow(rights);

        rights.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return rights;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private async Task<int> GetDefaultProductTypeIdAsync(string? formatCode, CancellationToken ct)
    {
        var type = formatCode != null
            ? await _db.CatalogProductTypes.FirstOrDefaultAsync(t => t.Code == formatCode, ct)
            : null;
        return type?.Id ?? (await _db.CatalogProductTypes.FirstAsync(ct)).Id;
    }
}
