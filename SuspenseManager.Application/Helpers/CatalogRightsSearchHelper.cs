using Common.Exceptions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Helpers;

/// <summary>
/// Поиск записей <see cref="CatalogProductRights"/> по критериям продукта и/или полей прав.
/// </summary>
public static class CatalogRightsSearchHelper
{
    /// <param name="excludeCatalogProductId">Исключить права этого продукта (продукт текущей группы).</param>
    /// <param name="combineProductFieldsWithAnd">
    /// false — OR по полям продукта (как раньше); true — все непустые поля продукта должны совпасть с одним продуктом.
    /// </param>
    public static async Task<List<CatalogProductRights>> SearchAsync(
        SuspenseManagerDbContext db,
        int? excludeCatalogProductId,
        string? artist,
        string? isrc,
        string? productName,
        string? barcode,
        string? rightsTerritoryCode,
        string? rightsDocNumber,
        bool combineProductFieldsWithAnd,
        CancellationToken ct)
    {
        artist = NullIfWhiteSpace(artist);
        isrc = NullIfWhiteSpace(isrc);
        productName = NullIfWhiteSpace(productName);
        barcode = NullIfWhiteSpace(barcode);
        rightsTerritoryCode = NullIfWhiteSpace(rightsTerritoryCode);
        rightsDocNumber = NullIfWhiteSpace(rightsDocNumber);

        var hasProduct =
            artist != null || isrc != null || productName != null || barcode != null;
        var hasRightsOnly = rightsTerritoryCode != null || rightsDocNumber != null;

        if (!hasProduct && !hasRightsOnly)
            throw new BusinessException(
                "Укажите хотя бы один параметр поиска", "SEARCH_CRITERIA_EMPTY");

        IQueryable<CatalogProductRights> rightsQuery;

        if (hasProduct)
        {
            var productsQuery = db.CatalogProducts
                .AsNoTracking()
                .Where(p => p.ArchiveLevel == 0);

            if (excludeCatalogProductId.HasValue)
                productsQuery = productsQuery.Where(p => p.Id != excludeCatalogProductId.Value);

            if (combineProductFieldsWithAnd)
            {
                if (isrc != null)
                    productsQuery = productsQuery.Where(p => p.Isrc != null && p.Isrc == isrc);
                if (barcode != null)
                    productsQuery = productsQuery.Where(p => p.Barcode != null && p.Barcode == barcode);
                if (productName != null)
                    productsQuery = productsQuery.Where(p =>
                        p.ProductName != null && p.ProductName.Contains(productName));
                if (artist != null)
                    productsQuery = productsQuery.Where(p =>
                        p.Artist != null && p.Artist.Contains(artist));
            }
            else
            {
                productsQuery = productsQuery.Where(p =>
                    (isrc != null && p.Isrc == isrc) ||
                    (barcode != null && p.Barcode == barcode) ||
                    (productName != null && p.ProductName != null && p.ProductName.Contains(productName)) ||
                    (artist != null && p.Artist != null && p.Artist.Contains(artist)));
            }

            var productIds = await productsQuery.Select(p => p.Id).Take(50).ToListAsync(ct);

            if (productIds.Count == 0)
                return [];

            rightsQuery = db.CatalogProductRights
                .AsNoTracking()
                .Where(r => productIds.Contains(r.CatalogProductId) && r.ArchiveLevel == 0);
        }
        else
        {
            rightsQuery = db.CatalogProductRights
                .AsNoTracking()
                .Where(r => r.ArchiveLevel == 0);

            if (excludeCatalogProductId.HasValue)
                rightsQuery = rightsQuery.Where(r => r.CatalogProductId != excludeCatalogProductId.Value);
        }

        if (rightsTerritoryCode != null)
        {
            var t = rightsTerritoryCode;
            rightsQuery = rightsQuery.Where(r =>
                r.TerritoryCode != null &&
                r.TerritoryCode.Equals(t, StringComparison.OrdinalIgnoreCase));
        }

        if (rightsDocNumber != null)
        {
            var d = rightsDocNumber;
            rightsQuery = rightsQuery.Where(r =>
                r.DocNumber != null && r.DocNumber.Contains(d, StringComparison.OrdinalIgnoreCase));
        }

        return await rightsQuery
            .Include(r => r.CompanySenderR)
            .Include(r => r.CompanyReceiverR)
            .Include(r => r.Territory)
            .Include(r => r.CatalogProduct)
            .OrderBy(r => r.CatalogProductId)
            .Take(200)
            .ToListAsync(ct);
    }

    private static string? NullIfWhiteSpace(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim();
    }
}
