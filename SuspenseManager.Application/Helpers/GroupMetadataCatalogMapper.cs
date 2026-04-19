using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Helpers;

/// <summary>
/// Перенос полей продукта каталога в метаданные группы (после привязки продукта).
/// </summary>
public static class GroupMetadataCatalogMapper
{
    public static async Task<GroupMetadata> GetOrCreateForGroupAsync(
        SuspenseManagerDbContext db,
        int groupId,
        CancellationToken ct)
    {
        var existing = await db.GroupMetadata.FirstOrDefaultAsync(m => m.SuspenseGroupId == groupId, ct);
        if (existing != null)
            return existing;

        var meta = new GroupMetadata
        {
            SuspenseGroupId = groupId,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        db.GroupMetadata.Add(meta);
        return meta;
    }

    public static void ApplyFromCatalogProduct(GroupMetadata meta, CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(product);

        meta.Title = EmptyToNull(product.ProductName);
        meta.Artist = EmptyToNull(product.Artist);
        meta.Isrc = EmptyToNull(product.Isrc);
        meta.Barcode = EmptyToNull(product.Barcode);
        meta.CatalogNumber = EmptyToNull(product.CatalogNumber);
        meta.Genre = EmptyToNull(product.Genre);
        meta.Description = product.Description;
        meta.ReleaseDate = product.ReleaseDate;
        meta.ProductTypeId = product.ProductTypeId;
        meta.ProductTypeCode = EmptyToNull(product.ProductType?.Code ?? product.ProductFormatCode);
        meta.ProductTypeDesc = EmptyToNull(product.ProductTypeDesc ?? product.ProductType?.Description);
        meta.CatalogProductId = product.Id;
        meta.Duration = null;
        meta.ChangeTime = DateTime.UtcNow;
    }

    private static string? EmptyToNull(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
