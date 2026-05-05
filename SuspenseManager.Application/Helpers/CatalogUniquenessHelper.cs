using Common.DTOs;
using Common.Exceptions;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Helpers;

public static class CatalogUniquenessHelper
{
    public static async Task EnsureUniqueCompanyFieldsAsync(
        SuspenseManagerDbContext db,
        string? legalName,
        string? companyCode,
        string? inn,
        string? bic,
        int? currentCompanyId,
        CancellationToken ct)
    {
        var errors = new List<ApiError>();
        var duplicateLabels = new List<string>();

        var normalizedLegalName = Normalize(legalName);
        if (normalizedLegalName is not null)
        {
            var duplicate = await db.Companies
                .AsNoTracking()
                .Where(c => c.ArchiveLevel == 0 && (!currentCompanyId.HasValue || c.Id != currentCompanyId.Value))
                .Where(c => c.LegalName != null && c.LegalName.Trim().ToUpper() == normalizedLegalName)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("юридическое наименование");
                errors.Add(new ApiError
                {
                    Field = "LegalName",
                    Message = $"Юридическое наименование уже используется в записи #{duplicate.Id}."
                });
            }
        }

        var normalizedCompanyCode = Normalize(companyCode);
        if (normalizedCompanyCode is not null)
        {
            var duplicate = await db.Companies
                .AsNoTracking()
                .Where(c => c.ArchiveLevel == 0 && (!currentCompanyId.HasValue || c.Id != currentCompanyId.Value))
                .Where(c => c.CompanyCode != null && c.CompanyCode.Trim().ToUpper() == normalizedCompanyCode)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("код компании");
                errors.Add(new ApiError
                {
                    Field = "CompanyCode",
                    Message = $"Код компании уже используется в записи #{duplicate.Id}."
                });
            }
        }

        var normalizedInn = Normalize(inn);
        if (normalizedInn is not null)
        {
            var duplicate = await db.Companies
                .AsNoTracking()
                .Where(c => c.ArchiveLevel == 0 && (!currentCompanyId.HasValue || c.Id != currentCompanyId.Value))
                .Where(c => c.Inn != null && c.Inn.Trim().ToUpper() == normalizedInn)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("ИНН");
                errors.Add(new ApiError
                {
                    Field = "Inn",
                    Message = $"ИНН уже используется в записи #{duplicate.Id}."
                });
            }
        }

        var normalizedBic = Normalize(bic);
        if (normalizedBic is not null)
        {
            var duplicate = await db.Companies
                .AsNoTracking()
                .Where(c => c.ArchiveLevel == 0 && (!currentCompanyId.HasValue || c.Id != currentCompanyId.Value))
                .Where(c => c.Bic != null && c.Bic.Trim().ToUpper() == normalizedBic)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("БИК");
                errors.Add(new ApiError
                {
                    Field = "Bic",
                    Message = $"БИК уже используется в записи #{duplicate.Id}."
                });
            }
        }

        if (errors.Count == 0)
            return;

        throw new ValidationException(
            $"Нельзя сохранить компанию: уже существует активная запись с совпадающим значением поля(полей): {string.Join(", ", duplicateLabels)}.",
            errors);
    }

    public static async Task EnsureUniqueTerritoryCodeAsync(
        SuspenseManagerDbContext db,
        string? territoryCode,
        int? currentTerritoryId,
        CancellationToken ct)
    {
        var normalized = Normalize(territoryCode);
        if (normalized is null)
            return;

        var duplicate = await db.Territories
            .AsNoTracking()
            .Where(t => t.ArchiveLevel == 0 && (!currentTerritoryId.HasValue || t.Id != currentTerritoryId.Value))
            .Where(t => t.TerritoryCode != null && t.TerritoryCode.Trim().ToUpper() == normalized)
            .Select(t => new { t.Id, t.TerritoryCode })
            .FirstOrDefaultAsync(ct);

        if (duplicate is null)
            return;

        throw new ValidationException(
            "Территория с таким кодом уже существует.",
            [new ApiError
            {
                Field = "TerritoryCode",
                Message = $"Код территории уже используется в записи #{duplicate.Id}."
            }]);
    }

    public static async Task EnsureUniqueCatalogProductIdentityAsync(
        SuspenseManagerDbContext db,
        string? isrc,
        string? barcode,
        string? catalogNumber,
        int? currentProductId,
        CancellationToken ct)
    {
        var errors = new List<ApiError>();
        var duplicateLabels = new List<string>();

        var normalizedIsrc = Normalize(isrc);
        if (normalizedIsrc is not null)
        {
            var duplicate = await db.CatalogProducts
                .AsNoTracking()
                .Where(p => p.ArchiveLevel == 0 && (!currentProductId.HasValue || p.Id != currentProductId.Value))
                .Where(p => p.Isrc != null && p.Isrc.Trim().ToUpper() == normalizedIsrc)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("ISRC");
                errors.Add(new ApiError
                {
                    Field = "Isrc",
                    Message = $"ISRC уже используется в продукте #{duplicate.Id}."
                });
            }
        }

        var normalizedBarcode = Normalize(barcode);
        if (normalizedBarcode is not null)
        {
            var duplicate = await db.CatalogProducts
                .AsNoTracking()
                .Where(p => p.ArchiveLevel == 0 && (!currentProductId.HasValue || p.Id != currentProductId.Value))
                .Where(p => p.Barcode != null && p.Barcode.Trim().ToUpper() == normalizedBarcode)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("баркод");
                errors.Add(new ApiError
                {
                    Field = "Barcode",
                    Message = $"Баркод уже используется в продукте #{duplicate.Id}."
                });
            }
        }

        var normalizedCatalogNumber = Normalize(catalogNumber);
        if (normalizedCatalogNumber is not null)
        {
            var duplicate = await db.CatalogProducts
                .AsNoTracking()
                .Where(p => p.ArchiveLevel == 0 && (!currentProductId.HasValue || p.Id != currentProductId.Value))
                .Where(p => p.CatalogNumber != null && p.CatalogNumber.Trim().ToUpper() == normalizedCatalogNumber)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                duplicateLabels.Add("каталожный номер");
                errors.Add(new ApiError
                {
                    Field = "CatalogNumber",
                    Message = $"Каталожный номер уже используется в продукте #{duplicate.Id}."
                });
            }
        }

        if (errors.Count == 0)
            return;

        throw new ValidationException(
            $"Нельзя сохранить продукт: уже существует активная запись с совпадающим значением поля(полей): {string.Join(", ", duplicateLabels)}.",
            errors);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToUpperInvariant();
    }
}
