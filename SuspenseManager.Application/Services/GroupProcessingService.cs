using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace Application.Services;

public class GroupProcessingService : IGroupProcessingService
{
    private readonly SuspenseManagerDbContext _db;

    public GroupProcessingService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<GroupMetadata> UpdateMetadataAsync(int groupId, UpdateGroupMetadataDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        var meta = await _db.GroupMetadata.FirstOrDefaultAsync(m => m.SuspenseGroupId == groupId, ct);
        if (meta == null)
        {
            meta = new GroupMetadata
            {
                SuspenseGroupId = groupId,
                CreateTime = DateTime.UtcNow
            };
            _db.GroupMetadata.Add(meta);
        }

        meta.CatalogNumber = dto.CatalogNumber ?? meta.CatalogNumber;
        meta.Barcode = dto.Barcode ?? meta.Barcode;
        meta.Isrc = dto.Isrc ?? meta.Isrc;
        meta.Artist = dto.Artist ?? meta.Artist;
        meta.Title = dto.Title ?? meta.Title;
        meta.Genre = dto.Genre ?? meta.Genre;
        meta.Description = dto.Description ?? meta.Description;
        meta.ProductTypeCode = dto.ProductTypeCode ?? meta.ProductTypeCode;
        meta.ProductTypeDesc = dto.ProductTypeDesc ?? meta.ProductTypeDesc;
        meta.Duration = dto.Duration ?? meta.Duration;
        meta.ReleaseDate = dto.ReleaseDate ?? meta.ReleaseDate;
        meta.ProductTypeId = dto.ProductTypeId ?? meta.ProductTypeId;
        meta.ChangeTime = DateTime.UtcNow;

        // Если устанавливается CatalogProductId — связываем группу с продуктом и меняем статус
        if (dto.CatalogProductId.HasValue && dto.CatalogProductId != meta.CatalogProductId)
        {
            var product = await _db.CatalogProducts
                .FirstOrDefaultAsync(p => p.Id == dto.CatalogProductId.Value && p.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

            meta.CatalogProductId = dto.CatalogProductId.Value;
            group.CatalogProductId = dto.CatalogProductId.Value;
            group.BusinessStatus = (int)BusinessStatus.InGroupNoRights;
            group.ChangeTime = DateTime.UtcNow;

            // Обновляем статус суспенсов группы
            await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.InGroupNoRights, dto.CatalogProductId.Value, ct);
        }

        // Связываем метаданные с группой
        if (group.MetaDataId == null)
        {
            await _db.SaveChangesAsync(ct); // сохраняем чтобы получить Id метаданных
            group.MetaDataId = meta.Id;
        }

        await _db.SaveChangesAsync(ct);
        return meta;
    }

    public async Task<GroupMetaRights> UpdateMetaRightsAsync(int groupId, UpdateGroupMetaRightsDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
            throw new BusinessException("Обновление метаправ доступно только для групп со статусом 'нет прав' (16)", "INVALID_STATUS");

        var metaRights = await _db.GroupMetaRights.FirstOrDefaultAsync(m => m.GroupId == groupId, ct);
        if (metaRights == null)
        {
            metaRights = new GroupMetaRights
            {
                GroupId = groupId,
                CreateTime = DateTime.UtcNow
            };
            _db.GroupMetaRights.Add(metaRights);
        }

        metaRights.DocNumber = dto.DocNumber ?? metaRights.DocNumber;
        metaRights.DocType = dto.DocType ?? metaRights.DocType;
        metaRights.DocDate = dto.DocDate ?? metaRights.DocDate;
        metaRights.DocStart = dto.DocStart ?? metaRights.DocStart;
        metaRights.DocEnd = dto.DocEnd ?? metaRights.DocEnd;
        metaRights.TerritoryId = dto.TerritoryId ?? metaRights.TerritoryId;
        metaRights.TerritoryCode = dto.TerritoryCode ?? metaRights.TerritoryCode;
        metaRights.TerritoryDesc = dto.TerritoryDesc ?? metaRights.TerritoryDesc;
        metaRights.SenderCompanyId = dto.SenderCompanyId ?? metaRights.SenderCompanyId;
        metaRights.ReceiverCompanyId = dto.ReceiverCompanyId ?? metaRights.ReceiverCompanyId;
        metaRights.Share = dto.Share ?? metaRights.Share;
        metaRights.ChangeTime = DateTime.UtcNow;

        if (group.MetaRightsId == null)
        {
            await _db.SaveChangesAsync(ct);
            group.MetaRightsId = metaRights.Id;
        }

        await _db.SaveChangesAsync(ct);
        return metaRights;
    }

    public async Task<CatalogProduct> QuickCatalogAsync(int groupId, CancellationToken ct = default)
    {
        var group = await _db.SuspenseGroups
            .Include(g => g.GroupMetaData)
            .Include(g => g.SuspenseLines.Where(s => s.ArchiveLevel == 0))
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoProduct)
            throw new BusinessException("Быстрая каталогизация доступна только для групп со статусом 'нет продукта' (15)", "INVALID_STATUS");

        // Собираем данные: приоритет метаданные > первый суспенс
        var meta = group.GroupMetaData;
        var firstSuspense = group.SuspenseLines.FirstOrDefault();

        if (meta == null && firstSuspense == null)
            throw new BusinessException("В группе нет данных для создания продукта (ни метаданных, ни суспенсов)", "NO_DATA_FOR_CATALOG");

        var isrc = meta?.Isrc ?? firstSuspense?.Isrc ?? string.Empty;
        var barcode = meta?.Barcode ?? firstSuspense?.Barcode ?? string.Empty;
        var catalogNumber = meta?.CatalogNumber ?? firstSuspense?.CatalogNumber ?? string.Empty;
        var artist = meta?.Artist ?? firstSuspense?.Artist;
        var title = meta?.Title ?? firstSuspense?.TrackTitle;
        var genre = meta?.Genre ?? firstSuspense?.Genre;
        var formatCode = meta?.ProductTypeCode ?? "DIGI";

        // Определяем ProductTypeId
        int productTypeId;
        if (meta?.ProductTypeId.HasValue == true)
        {
            productTypeId = meta.ProductTypeId.Value;
        }
        else
        {
            var productType = await _db.CatalogProductTypes
                .FirstOrDefaultAsync(t => t.Code == formatCode, ct)
                ?? await _db.CatalogProductTypes.FirstAsync(ct);
            productTypeId = productType.Id;
        }

        // Создаём продукт из данных группы
        var product = new CatalogProduct
        {
            ProductName = title,
            Artist = artist,
            Isrc = isrc,
            Barcode = barcode,
            CatalogNumber = catalogNumber,
            ProductFormatCode = formatCode,
            Genre = genre,
            Description = meta?.Description,
            ReleaseDate = meta?.ReleaseDate,
            ProductTypeId = productTypeId,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };

        _db.CatalogProducts.Add(product);
        await _db.SaveChangesAsync(ct);

        // Создаём пустую запись прав
        var emptyRights = new CatalogProductRights
        {
            CatalogProductId = product.Id,
            CompanySender = string.Empty,
            CompanyReceiver = string.Empty,
            CompanySenderId = 1,
            CompanyReceiverId = 1,
            TerritoryCode = string.Empty,
            TerritoryDesc = string.Empty,
            TerritoryId = 1,
            DocStart = DateOnly.FromDateTime(DateTime.UtcNow),
            DocEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
        _db.CatalogProductRights.Add(emptyRights);

        // Обновляем группу
        group.CatalogProductId = product.Id;
        group.BusinessStatus = (int)BusinessStatus.InGroupNoRights;
        group.ChangeTime = DateTime.UtcNow;

        // Обновляем суспенсы
        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.InGroupNoRights, product.Id, ct);

        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<PagedResponse<CatalogProduct>> GetPossibleProductsAsync(int groupId, PagedRequest request, CancellationToken ct = default)
    {
        var group = await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.GroupMetaData)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

        // Берём данные для поиска из метаданных или первого суспенса
        var firstSuspense = await _db.SuspenseLines
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GroupId == groupId && s.ArchiveLevel == 0, ct);

        var isrc = group.GroupMetaData?.Isrc ?? firstSuspense?.Isrc;
        var barcode = group.GroupMetaData?.Barcode ?? firstSuspense?.Barcode;
        var title = group.GroupMetaData?.Title ?? firstSuspense?.TrackTitle;
        var artist = group.GroupMetaData?.Artist ?? firstSuspense?.Artist;

        var query = _db.CatalogProducts
            .AsNoTracking()
            .Where(p => p.ArchiveLevel == 0);

        // Фильтруем по хотя бы одному совпадению
        query = query.Where(p =>
            (!string.IsNullOrEmpty(isrc) && p.Isrc == isrc) ||
            (!string.IsNullOrEmpty(barcode) && p.Barcode == barcode) ||
            (!string.IsNullOrEmpty(title) && p.ProductName != null && p.ProductName.Contains(title)) ||
            (!string.IsNullOrEmpty(artist) && p.Artist != null && p.Artist.Contains(artist)));

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<SuspenseGroup> SendToBackOfficeAsync(int groupId, SendToBackOfficeDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        var newStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.InGroupNoProduct => (int)BusinessStatus.BackOfficeNoProduct,
            (int)BusinessStatus.InGroupNoRights => (int)BusinessStatus.BackOfficeNoRights,
            _ => throw new BusinessException("Отправка в бэк-офис доступна только для групп со статусом 15 или 16", "INVALID_STATUS")
        };

        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<SuspenseGroup> PostponeAsync(int groupId, PostponeGroupDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        var newStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.InGroupNoProduct => (int)BusinessStatus.PostponedNoProduct,
            (int)BusinessStatus.InGroupNoRights => (int)BusinessStatus.PostponedNoRights,
            _ => throw new BusinessException("Откладывание доступно только для групп со статусом 15 или 16", "INVALID_STATUS")
        };

        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<GroupMetadata?> GetMetadataAsync(int groupId, CancellationToken ct = default)
    {
        return await _db.GroupMetadata
            .AsNoTracking()
            .Include(m => m.CatalogProduct)
            .Include(m => m.ProductType)
            .FirstOrDefaultAsync(m => m.SuspenseGroupId == groupId, ct);
    }

    public async Task<GroupMetaRights?> GetMetaRightsAsync(int groupId, CancellationToken ct = default)
    {
        return await _db.GroupMetaRights
            .AsNoTracking()
            .Include(m => m.SenderCompany)
            .Include(m => m.ReceiverCompany)
            .Include(m => m.Territory)
            .FirstOrDefaultAsync(m => m.GroupId == groupId, ct);
    }

    public async Task UngroupAsync(int groupId, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        var revertStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.InGroupNoProduct => (int)BusinessStatus.NoProduct,
            (int)BusinessStatus.InGroupNoRights => (int)BusinessStatus.NoRights,
            _ => throw new BusinessException("Разгруппировка доступна только для групп со статусом 15 или 16", "INVALID_STATUS")
        };

        // Архивируем группу (soft delete)
        group.ArchiveLevel = 1;
        group.ArchiveTime = DateTime.UtcNow;
        group.ChangeTime = DateTime.UtcNow;

        // Возвращаем суспенсы в исходный статус
        var suspenses = await _db.SuspenseLines
            .Where(s => s.GroupId == groupId)
            .ToListAsync(ct);

        foreach (var s in suspenses)
        {
            s.BusinessStatus = revertStatus;
            s.GroupId = null;
            s.ChangeTime = DateTime.UtcNow;
            // ProductId НЕ обнуляется — связь с продуктом сохраняется (бизнес-правило 5)
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SuspenseGroup> LinkProductAsync(int groupId, LinkProductDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoProduct)
            throw new BusinessException("Привязка продукта доступна только для групп со статусом 'нет продукта' (15)", "INVALID_STATUS");

        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

        group.CatalogProductId = product.Id;
        group.BusinessStatus = (int)BusinessStatus.InGroupNoRights;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.InGroupNoRights, product.Id, ct);
        await _db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetPostponedGroupsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Where(g => g.ArchiveLevel == 0 &&
                (g.BusinessStatus == (int)BusinessStatus.PostponedNoProduct ||
                 g.BusinessStatus == (int)BusinessStatus.PostponedNoRights));

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<SuspenseGroup> ReturnFromPostponedAsync(int groupId, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        var newStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.PostponedNoProduct => (int)BusinessStatus.InGroupNoProduct,
            (int)BusinessStatus.PostponedNoRights => (int)BusinessStatus.InGroupNoRights,
            _ => throw new BusinessException("Возврат доступен только для отложенных групп", "INVALID_STATUS")
        };

        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        return group;
    }

    private async Task<SuspenseGroup> GetGroupOrThrowAsync(int groupId, CancellationToken ct)
    {
        return await _db.SuspenseGroups
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");
    }

    public async Task<SuspenseGroup> ValidateGroupAsync(int groupId, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        group.BusinessStatus = (int)BusinessStatus.Validated;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.Validated, null, ct);
        await _db.SaveChangesAsync(ct);

        return group;
    }

    private async Task UpdateSuspenseStatusAsync(int groupId, int newStatus, int? productId, CancellationToken ct)
    {
        var suspenses = await _db.SuspenseLines
            .Where(s => s.GroupId == groupId)
            .ToListAsync(ct);

        foreach (var s in suspenses)
        {
            s.BusinessStatus = newStatus;
            s.ChangeTime = DateTime.UtcNow;
            if (productId.HasValue)
                s.ProductId = productId.Value;
        }
    }
}
