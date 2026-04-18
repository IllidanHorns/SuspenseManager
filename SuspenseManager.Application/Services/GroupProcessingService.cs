using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;
using BackOfficeTaskStatus = Models.Enums.BackOfficeTaskStatus;

namespace Application.Services;

public class GroupProcessingService : IGroupProcessingService
{
    private readonly SuspenseManagerDbContext _db;
    private readonly IAuditService _audit;

    public GroupProcessingService(SuspenseManagerDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<GroupMetadata> UpdateMetadataAsync(int groupId, UpdateGroupMetadataDto dto, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

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

        // Если устанавливается CatalogProductId — связываем группу с продуктом
        if (dto.CatalogProductId.HasValue && dto.CatalogProductId != meta.CatalogProductId)
        {
            // Для BO-групп привязка продукта выполняется только через BO-эндпоинт link-product,
            // иначе BO-задание не получит правильного жизненного цикла (Completed / смена статуса)
            if (group.BusinessStatus == (int)BusinessStatus.BackOfficeNoProduct ||
                group.BusinessStatus == (int)BusinessStatus.BackOfficeNoRights)
                throw new BusinessException(
                    "Для групп в бэк-офисе привязка продукта выполняется через эндпоинт link-product задания",
                    "USE_BO_LINK_PRODUCT");

            var product = await _db.CatalogProducts
                .FirstOrDefaultAsync(p => p.Id == dto.CatalogProductId.Value && p.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

            // Если у продукта уже есть права в каталоге — сразу 88, иначе 16
            var rightsExist = await _db.CatalogProductRights
                .AnyAsync(r => r.CatalogProductId == product.Id && r.ArchiveLevel == 0, ct);

            var newStatus = rightsExist
                ? (int)BusinessStatus.Validated
                : (int)BusinessStatus.InGroupNoRights;

            var prevStatus = group.BusinessStatus;
            meta.CatalogProductId = dto.CatalogProductId.Value;
            group.CatalogProductId = dto.CatalogProductId.Value;
            group.BusinessStatus = newStatus;
            group.ChangeTime = DateTime.UtcNow;

            await UpdateSuspenseStatusAsync(groupId, newStatus, dto.CatalogProductId.Value, ct);
            await _db.SaveChangesAsync(ct);
            await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
            await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
        }

        if (group.MetaDataId == null)
        {
            await _db.SaveChangesAsync(ct);
            group.MetaDataId = meta.Id;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return meta;
    }

    public async Task<GroupMetaRights> UpdateMetaRightsAsync(int groupId, UpdateGroupMetaRightsDto dto, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights
            && group.BusinessStatus != (int)BusinessStatus.BackOfficeNoRights)
        {
            throw new BusinessException(
                "Обновление метаправ доступно только для групп со статусом 'нет прав' (16 или 320)",
                "INVALID_STATUS");
        }

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
        await tx.CommitAsync(ct);
        return metaRights;
    }

    public async Task<CatalogProduct> QuickCatalogAsync(int groupId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await _db.SuspenseGroups
            .Include(g => g.GroupMetaData)
            .Include(g => g.SuspenseLines.Where(s => s.ArchiveLevel == 0))
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoProduct)
        {
            throw new BusinessException("Быстрая каталогизация доступна только для групп со статусом 'нет продукта' (15)", "INVALID_STATUS");
        }

        var meta = group.GroupMetaData;
        var firstSuspense = group.SuspenseLines.FirstOrDefault();

        if (meta == null && firstSuspense == null)
        {
            throw new BusinessException("В группе нет данных для создания продукта (ни метаданных, ни суспенсов)", "NO_DATA_FOR_CATALOG");
        }

        var isrc = meta?.Isrc ?? firstSuspense?.Isrc ?? string.Empty;
        var barcode = meta?.Barcode ?? firstSuspense?.Barcode ?? string.Empty;
        var catalogNumber = meta?.CatalogNumber ?? firstSuspense?.CatalogNumber ?? string.Empty;
        var artist = meta?.Artist ?? firstSuspense?.Artist;
        var title = meta?.Title ?? firstSuspense?.TrackTitle;
        var genre = meta?.Genre ?? firstSuspense?.Genre;
        var formatCode = meta?.ProductTypeCode ?? "DIGI";

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

        // Новый продукт прав никогда не имеет — всегда переходим в статус 16
        group.CatalogProductId = product.Id;
        group.BusinessStatus = (int)BusinessStatus.InGroupNoRights;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.InGroupNoRights, product.Id, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, (int)BusinessStatus.InGroupNoProduct, (int)BusinessStatus.InGroupNoRights, ct);
        await _audit.LogGroupLinesAsync(groupId, (int)BusinessStatus.InGroupNoProduct, (int)BusinessStatus.InGroupNoRights, ct);
        await tx.CommitAsync(ct);
        return product;
    }

    public async Task<PagedResponse<CatalogProduct>> GetPossibleProductsAsync(int groupId, PagedRequest request, CancellationToken ct = default)
    {
        var group = await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.GroupMetaData)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

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

        query = query.Where(p =>
            (!string.IsNullOrEmpty(isrc) && p.Isrc == isrc) ||
            (!string.IsNullOrEmpty(barcode) && p.Barcode == barcode) ||
            (!string.IsNullOrEmpty(title) && p.ProductName != null && p.ProductName.Contains(title)) ||
            (!string.IsNullOrEmpty(artist) && p.Artist != null && p.Artist.Contains(artist)));

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<SuspenseGroup> LinkProductAsync(int groupId, LinkProductDto dto, CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoProduct)
        {
            throw new BusinessException("Привязка продукта доступна только для групп со статусом 'нет продукта' (15)", "INVALID_STATUS");
        }

        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

        // Проверяем наличие прав у продукта — если есть, сразу 88, иначе 16
        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == product.Id && r.ArchiveLevel == 0, ct);

        var newStatus = rightsExist
            ? (int)BusinessStatus.Validated
            : (int)BusinessStatus.InGroupNoRights;

        group.CatalogProductId = product.Id;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, product.Id, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, (int)BusinessStatus.InGroupNoProduct, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, (int)BusinessStatus.InGroupNoProduct, newStatus, ct);
        return group;
    }

    public async Task<SuspenseGroup> ValidateGroupAsync(int groupId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
        {
            throw new BusinessException(
                "Валидация доступна только для групп со статусом 'нет прав' (16)",
                "INVALID_STATUS");
        }

        if (!group.CatalogProductId.HasValue)
        {
            throw new BusinessException(
                "Группа не привязана к продукту каталога",
                "NO_PRODUCT");
        }

        // Шаг 1: Проверяем — есть ли уже права у продукта в каталоге
        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == group.CatalogProductId.Value && r.ArchiveLevel == 0, ct);

        if (!rightsExist)
        {
            // Шаг 2: Права не найдены — пробуем создать из GroupMetaRights
            ValidateMetaRightsFields(group.GroupMetaRights);
            await CreateCatalogRightsFromMetaAsync(group.CatalogProductId.Value, group.GroupMetaRights!, ct);
        }

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = (int)BusinessStatus.Validated;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.Validated, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, (int)BusinessStatus.Validated, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, (int)BusinessStatus.Validated, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    public async Task<List<CatalogProductRights>> SearchCatalogRightsAsync(
        int groupId,
        string? artist,
        string? isrc,
        string? productName,
        string? barcode,
        CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
        {
            throw new BusinessException(
                "Поиск прав доступен только для групп со статусом 'нет прав' (16)",
                "INVALID_STATUS");
        }

        if (string.IsNullOrWhiteSpace(artist) &&
            string.IsNullOrWhiteSpace(isrc) &&
            string.IsNullOrWhiteSpace(productName) &&
            string.IsNullOrWhiteSpace(barcode))
        {
            throw new BusinessException("Укажите хотя бы один параметр поиска", "SEARCH_CRITERIA_EMPTY");
        }

        var productsQuery = _db.CatalogProducts
            .AsNoTracking()
            .Where(p => p.ArchiveLevel == 0);

        // Исключаем продукт самой группы, чтобы не показывать его права
        if (group.CatalogProductId.HasValue)
            productsQuery = productsQuery.Where(p => p.Id != group.CatalogProductId.Value);

        productsQuery = productsQuery.Where(p =>
            (!string.IsNullOrEmpty(isrc) && p.Isrc == isrc) ||
            (!string.IsNullOrEmpty(barcode) && p.Barcode == barcode) ||
            (!string.IsNullOrEmpty(productName) && p.ProductName != null && p.ProductName.Contains(productName)) ||
            (!string.IsNullOrEmpty(artist) && p.Artist != null && p.Artist.Contains(artist)));

        var productIds = await productsQuery.Select(p => p.Id).Take(50).ToListAsync(ct);

        if (productIds.Count == 0)
            return [];

        return await _db.CatalogProductRights
            .AsNoTracking()
            .Include(r => r.CompanySenderR)
            .Include(r => r.CompanyReceiverR)
            .Include(r => r.Territory)
            .Include(r => r.CatalogProduct)
            .Where(r => productIds.Contains(r.CatalogProductId) && r.ArchiveLevel == 0)
            .ToListAsync(ct);
    }

    public async Task<SuspenseGroup> CopyRightsToProductAsync(int groupId, int rightsId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
        {
            throw new BusinessException(
                "Копирование прав доступно только для групп со статусом 'нет прав' (16)",
                "INVALID_STATUS");
        }

        if (!group.CatalogProductId.HasValue)
        {
            throw new BusinessException("Группа не привязана к продукту каталога", "NO_PRODUCT");
        }

        var source = await _db.CatalogProductRights
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rightsId && r.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Запись прав не найдена", "RIGHTS_NOT_FOUND", 404);

        // Не создаём дубль: одинаковые договор + территория + компания-отправитель
        var alreadyExists = await _db.CatalogProductRights.AnyAsync(r =>
            r.CatalogProductId == group.CatalogProductId.Value &&
            r.DocNumber == source.DocNumber &&
            r.TerritoryCode == source.TerritoryCode &&
            r.CompanySenderId == source.CompanySenderId &&
            r.ArchiveLevel == 0, ct);

        if (!alreadyExists)
        {
            _db.CatalogProductRights.Add(new CatalogProductRights
            {
                CatalogProductId  = group.CatalogProductId.Value,
                DocNumber         = source.DocNumber,
                CompanySender     = source.CompanySender,
                CompanyReceiver   = source.CompanyReceiver,
                CompanySenderId   = source.CompanySenderId,
                CompanyReceiverId = source.CompanyReceiverId,
                Share             = source.Share,
                TerritoryCode     = source.TerritoryCode,
                TerritoryDesc     = source.TerritoryDesc,
                TerritoryId       = source.TerritoryId,
                DocStart          = source.DocStart,
                DocEnd            = source.DocEnd,
                CreateTime        = DateTime.UtcNow,
                ArchiveLevel      = 0
            });
            await _db.SaveChangesAsync(ct);
        }

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = (int)BusinessStatus.Validated;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, (int)BusinessStatus.Validated, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, (int)BusinessStatus.Validated, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, (int)BusinessStatus.Validated, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    public async Task<SuspenseGroup> SendToBackOfficeAsync(int groupId, SendToBackOfficeDto dto, int accountId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        var newStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.InGroupNoProduct => (int)BusinessStatus.BackOfficeNoProduct,
            (int)BusinessStatus.InGroupNoRights => (int)BusinessStatus.BackOfficeNoRights,
            _ => throw new BusinessException("Отправка в бэк-офис доступна только для групп со статусом 15 или 16", "INVALID_STATUS")
        };

        // Защита от создания дублирующего задания (группа уже в BO)
        var hasActiveTask = await _db.BackOfficeTasks
            .AnyAsync(t => t.GroupId == groupId && t.ArchiveLevel == 0, ct);
        if (hasActiveTask)
            throw new BusinessException(
                "Для данной группы уже существует активное задание в бэк-офисе",
                "TASK_ALREADY_EXISTS");

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        _db.BackOfficeTasks.Add(new BackOfficeTask
        {
            GroupId = groupId,
            CreatedByAccountId = accountId,
            ProblemDescription = dto.ProblemDescription,
            TaskStatus = (int)BackOfficeTaskStatus.Open,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
        });

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
        await tx.CommitAsync(ct);
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

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
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
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        var revertStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.InGroupNoProduct    => (int)BusinessStatus.NoProduct,
            (int)BusinessStatus.InGroupNoRights     => (int)BusinessStatus.NoRights,
            (int)BusinessStatus.PostponedNoProduct  => (int)BusinessStatus.NoProduct,
            (int)BusinessStatus.PostponedNoRights   => (int)BusinessStatus.NoRights,
            _ => throw new BusinessException(
                "Разгруппировка доступна только для групп со статусом 15, 16, 30 или 32",
                "INVALID_STATUS")
        };

        var prevStatus = group.BusinessStatus;

        await _audit.LogGroupLinesAsync(groupId, prevStatus, revertStatus, ct);

        group.ArchiveLevel = 1;
        group.ArchiveTime = DateTime.UtcNow;
        group.ChangeTime = DateTime.UtcNow;

        if (group.MetaDataId.HasValue)
        {
            var meta = await _db.GroupMetadata
                .FirstOrDefaultAsync(m => m.Id == group.MetaDataId.Value, ct);
            if (meta != null)
            {
                meta.ArchiveLevel = 1;
                meta.ArchiveTime = DateTime.UtcNow;
            }
        }

        if (group.MetaRightsId.HasValue)
        {
            var metaRights = await _db.GroupMetaRights
                .FirstOrDefaultAsync(r => r.Id == group.MetaRightsId.Value, ct);
            if (metaRights != null)
            {
                metaRights.ArchiveLevel = 1;
                metaRights.ArchiveTime = DateTime.UtcNow;
            }
        }

        var links = await _db.SuspenseGroupLinks
            .Where(l => l.SuspenseGroupId == groupId && l.ArchiveLevel == 0)
            .ToListAsync(ct);

        foreach (var link in links)
        {
            link.ArchiveLevel = 1;
            link.ArchiveTime = DateTime.UtcNow;
        }

        var suspenses = await _db.SuspenseLines
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0)
            .ToListAsync(ct);

        foreach (var s in suspenses)
        {
            s.BusinessStatus = revertStatus;
            s.GroupId = null;
            s.ChangeTime = DateTime.UtcNow;
            // ProductId НЕ обнуляется — связь с продуктом сохраняется (бизнес-правило 5)
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
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

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
        return group;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<SuspenseGroup> GetGroupOrThrowAsync(int groupId, CancellationToken ct)
    {
        return await _db.SuspenseGroups
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");
    }

    private async Task UpdateSuspenseStatusAsync(int groupId, int newStatus, int? productId, CancellationToken ct)
    {
        var suspenses = await _db.SuspenseLines
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0)
            .ToListAsync(ct);

        foreach (var s in suspenses)
        {
            s.BusinessStatus = newStatus;
            s.ChangeTime = DateTime.UtcNow;
            if (productId.HasValue)
                s.ProductId = productId.Value;
        }
    }

    /// <summary>
    /// Проверяет, что все обязательные поля GroupMetaRights заполнены для создания записи прав в каталоге.
    /// </summary>
    private static void ValidateMetaRightsFields(GroupMetaRights? meta)
    {
        if (meta == null)
        {
            throw new BusinessException(
                "Метаправа группы не заполнены. Заполните права вручную или скопируйте из каталога.",
                "META_RIGHTS_EMPTY");
        }

        var missing = new List<string>();

        if (!meta.SenderCompanyId.HasValue)   missing.Add("Компания-отправитель");
        if (!meta.ReceiverCompanyId.HasValue)  missing.Add("Компания-получатель");
        if (!meta.TerritoryId.HasValue)        missing.Add("Территория");
        if (string.IsNullOrWhiteSpace(meta.TerritoryCode)) missing.Add("Код территории");
        if (!meta.DocStart.HasValue)           missing.Add("Дата начала договора");
        if (!meta.DocEnd.HasValue)             missing.Add("Дата окончания договора");
        if (!meta.Share.HasValue)              missing.Add("Доля (%)");

        if (missing.Count > 0)
        {
            throw new BusinessException(
                $"Не заполнены обязательные поля метаправ: {string.Join(", ", missing)}",
                "META_RIGHTS_INCOMPLETE");
        }
    }

    /// <summary>
    /// Создаёт запись CatalogProductRights на основе GroupMetaRights.
    /// Вызывается только после прохождения ValidateMetaRightsFields.
    /// </summary>
    private async Task CreateCatalogRightsFromMetaAsync(int productId, GroupMetaRights meta, CancellationToken ct)
    {
        var sender = await _db.Companies
            .AsNoTracking()
            .FirstAsync(c => c.Id == meta.SenderCompanyId!.Value, ct);

        var receiver = await _db.Companies
            .AsNoTracking()
            .FirstAsync(c => c.Id == meta.ReceiverCompanyId!.Value, ct);

        _db.CatalogProductRights.Add(new CatalogProductRights
        {
            CatalogProductId  = productId,
            DocNumber         = meta.DocNumber,
            CompanySender     = sender.ShortName,
            CompanyReceiver   = receiver.ShortName,
            CompanySenderId   = meta.SenderCompanyId!.Value,
            CompanyReceiverId = meta.ReceiverCompanyId!.Value,
            Share             = meta.Share!.Value,
            TerritoryCode     = meta.TerritoryCode!,
            TerritoryDesc     = meta.TerritoryDesc ?? string.Empty,
            TerritoryId       = meta.TerritoryId!.Value,
            DocStart          = meta.DocStart!.Value,
            DocEnd            = meta.DocEnd!.Value,
            CreateTime        = DateTime.UtcNow,
            ArchiveLevel      = 0
        });
        await _db.SaveChangesAsync(ct);
    }
}
