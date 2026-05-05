using Application.Helpers;
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

        if (group.CatalogProductId.HasValue)
        {
            throw new BusinessException(
                "Продукт уже привязан к каталогу: метаданные продукта отображаются из каталога и не редактируются отдельно.",
                "METADATA_PRODUCT_READONLY");
        }

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

        // Привязка продукта через метаданные: поля продукта полностью берутся из каталога
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
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(p => p.Id == dto.CatalogProductId.Value && p.ArchiveLevel == 0, ct)
                ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

            var rightsExist = await _db.CatalogProductRights
                .AnyAsync(r => r.CatalogProductId == product.Id && r.ArchiveLevel == 0, ct);

            var newStatus = rightsExist
                ? (int)BusinessStatus.Validated
                : (int)BusinessStatus.InGroupNoRights;

            var prevStatus = group.BusinessStatus;
            GroupMetadataCatalogMapper.ApplyFromCatalogProduct(meta, product);
            group.CatalogProductId = dto.CatalogProductId.Value;
            group.BusinessStatus = newStatus;
            group.ChangeTime = DateTime.UtcNow;

            await UpdateSuspenseStatusAsync(groupId, newStatus, dto.CatalogProductId.Value, ct);
            await _db.SaveChangesAsync(ct);
            await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
            await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
        }
        else
        {
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
        EnsureCompleteMetadataForQuickCatalog(meta);

        var firstSuspense = group.SuspenseLines.FirstOrDefault();

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
                ?? await _db.CatalogProductTypes.FirstOrDefaultAsync(ct)
                ?? throw new BusinessException("В системе не настроены типы продуктов.", "NO_PRODUCT_TYPES");
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

        var productWithType = await _db.CatalogProducts
            .Include(p => p.ProductType)
            .FirstAsync(p => p.Id == product.Id, ct);
        var metaForSync = await GroupMetadataCatalogMapper.GetOrCreateForGroupAsync(_db, groupId, ct);
        GroupMetadataCatalogMapper.ApplyFromCatalogProduct(metaForSync, productWithType);

        // Новый продукт прав никогда не имеет — всегда переходим в статус 16
        group.CatalogProductId = product.Id;
        group.BusinessStatus = (int)BusinessStatus.InGroupNoRights;
        group.ChangeTime = DateTime.UtcNow;

        if (group.MetaDataId == null)
            group.MetaDataId = metaForSync.Id;

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
            .Include(p => p.ProductType)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

        EnsureCatalogProductIdentityCompleteForLink(product);

        // Проверяем наличие прав у продукта — если есть, сразу 88, иначе 16
        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == product.Id && r.ArchiveLevel == 0, ct);

        var newStatus = rightsExist
            ? (int)BusinessStatus.Validated
            : (int)BusinessStatus.InGroupNoRights;

        var meta = await GroupMetadataCatalogMapper.GetOrCreateForGroupAsync(_db, groupId, ct);
        GroupMetadataCatalogMapper.ApplyFromCatalogProduct(meta, product);

        group.CatalogProductId = product.Id;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        await UpdateSuspenseStatusAsync(groupId, newStatus, product.Id, ct);
        await _db.SaveChangesAsync(ct);

        if (group.MetaDataId == null)
        {
            group.MetaDataId = meta.Id;
            await _db.SaveChangesAsync(ct);
        }
        await _audit.LogGroupAsync(groupId, (int)BusinessStatus.InGroupNoProduct, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, (int)BusinessStatus.InGroupNoProduct, newStatus, ct);
        return group;
    }

    public async Task<SuspenseGroup> CreateRightsAsync(int groupId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
            throw new BusinessException(
                "Создание прав доступно только для групп со статусом 'нет прав' (16)",
                "INVALID_STATUS");

        if (!group.CatalogProductId.HasValue)
            throw new BusinessException("Группа не привязана к продукту каталога", "NO_PRODUCT");

        // Проверяем нет ли уже прав (защита от гонки / повторного нажатия)
        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == group.CatalogProductId.Value && r.ArchiveLevel == 0, ct);

        if (!rightsExist)
        {
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
        string? rightsTerritoryCode,
        string? rightsDocNumber,
        bool combineProductFieldsWithAnd,
        CancellationToken ct = default)
    {
        var group = await GetGroupOrThrowAsync(groupId, ct);

        if (group.BusinessStatus != (int)BusinessStatus.InGroupNoRights)
        {
            throw new BusinessException(
                "Поиск прав доступен только для групп со статусом 'нет прав' (16)",
                "INVALID_STATUS");
        }

        return await CatalogRightsSearchHelper.SearchAsync(
            _db,
            group.CatalogProductId,
            artist,
            isrc,
            productName,
            barcode,
            rightsTerritoryCode,
            rightsDocNumber,
            combineProductFieldsWithAnd,
            ct);
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
        group.PostponeReason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();

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

    public async Task<PagedResponse<SuspenseGroup>> GetPostponedGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Include(g => g.CatalogProduct)
            .Where(g => g.ArchiveLevel == 0 &&
                (g.BusinessStatus == (int)BusinessStatus.PostponedNoProduct ||
                 g.BusinessStatus == (int)BusinessStatus.PostponedNoRights));

        if (request.OnlyMine && currentAccountId > 0)
        {
            query = query.Where(g => g.AccountId == currentAccountId);
        }

        if (!string.IsNullOrWhiteSpace(request.Artist))
        {
            var v = request.Artist;
            query = query.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.Artist != null && g.CatalogProduct.Artist.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Artist != null && g.GroupMetaData.Artist.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var v = request.Title;
            query = query.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.ProductName != null && g.CatalogProduct.ProductName.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Title != null && g.GroupMetaData.Title.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(request.Isrc))
        {
            var v = request.Isrc;
            query = query.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.Isrc != null && g.CatalogProduct.Isrc.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Isrc != null && g.GroupMetaData.Isrc.Contains(v)));
        }

        if (request.CountMin.HasValue)
        {
            int min = request.CountMin.Value;
            query = query.Where(g => _db.SuspenseLines.Count(s => s.GroupId == g.Id && s.ArchiveLevel == 0) >= min);
        }

        if (request.CountMax.HasValue)
        {
            int max = request.CountMax.Value;
            query = query.Where(g => _db.SuspenseLines.Count(s => s.GroupId == g.Id && s.ArchiveLevel == 0) <= max);
        }

        if (request.RevenueMin.HasValue)
        {
            double min = (double)request.RevenueMin.Value;
            query = query.Where(g => _db.SuspenseLines
                .Where(s => s.GroupId == g.Id && s.ArchiveLevel == 0)
                .Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate) >= min);
        }

        if (request.RevenueMax.HasValue)
        {
            double max = (double)request.RevenueMax.Value;
            query = query.Where(g => _db.SuspenseLines
                .Where(s => s.GroupId == g.Id && s.ArchiveLevel == 0)
                .Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate) <= max);
        }

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillGroupAggregatesAsync(result.Items, ct);
        return result;
    }

    private async Task FillGroupAggregatesAsync(List<SuspenseGroup> groups, CancellationToken ct)
    {
        if (groups.Count == 0) return;
        var ids = groups.Select(g => g.Id).ToList();

        var aggs = await _db.SuspenseLines
            .Where(s => s.GroupId.HasValue && ids.Contains(s.GroupId.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId!.Value)
            .Select(g => new
            {
                GroupId = g.Key,
                Count = g.Count(),
                RevenueDouble = g.Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate)
            })
            .ToDictionaryAsync(x => x.GroupId, ct);

        foreach (var group in groups)
        {
            if (aggs.TryGetValue(group.Id, out var agg))
            {
                group.SuspenseCount = agg.Count;
                group.RevenueRub = (decimal)agg.RevenueDouble;
            }
            else
            {
                group.SuspenseCount = 0;
                group.RevenueRub = 0;
            }
        }
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
        group.PostponeReason = null;

        await UpdateSuspenseStatusAsync(groupId, newStatus, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(groupId, prevStatus, newStatus, ct);
        await _audit.LogGroupLinesAsync(groupId, prevStatus, newStatus, ct);
        return group;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Быстрая каталогизация: все поля должны быть заданы именно в метаданных группы (каталога ещё нет).
    /// </summary>
    private static void EnsureCompleteMetadataForQuickCatalog(GroupMetadata? meta)
    {
        if (meta == null)
        {
            throw new BusinessException(
                "Для быстрой каталогизации заполните метаданные продукта на вкладке «Метаданные»: " +
                "название, исполнитель, ISRC, баркод, каталожный номер.",
                "METADATA_INCOMPLETE");
        }

        var missing = CollectMissingIdentityFields(
            meta.Title, meta.Artist, meta.Isrc, meta.Barcode, meta.CatalogNumber);
        if (missing.Count == 0)
            return;

        throw new BusinessException(
            "Для быстрой каталогизации дозаполните метаданные продукта (вкладка «Метаданные»). " +
            $"Не указано: {string.Join(", ", missing)}.",
            "METADATA_INCOMPLETE");
    }

    /// <summary>
    /// Привязка из «возможных продуктов»: в карточке каталога должны быть те же поля, что обязательны при быстрой каталогизации.
    /// </summary>
    private static void EnsureCatalogProductIdentityCompleteForLink(CatalogProduct product)
    {
        if (!CatalogProductIdentity.IsIncomplete(product))
            return;

        var missing = CollectMissingIdentityFields(
            product.ProductName,
            product.Artist,
            product.Isrc,
            product.Barcode,
            product.CatalogNumber);

        throw new BusinessException(
            "У выбранного продукта в каталоге не заполнены обязательные поля: " +
            string.Join(", ", missing) +
            ". Связывание недоступно. Доработку карточки продукта выполняет бэк-офис — отправьте группу в бэк-офис или обратитесь к ответственным за каталог.",
            "CATALOG_PRODUCT_INCOMPLETE");
    }

    private static List<string> CollectMissingIdentityFields(
        string? title, string? artist, string? isrc, string? barcode, string? catalogNumber)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(title))
            missing.Add("название");
        if (string.IsNullOrWhiteSpace(artist))
            missing.Add("исполнитель");
        if (string.IsNullOrWhiteSpace(isrc))
            missing.Add("ISRC");
        if (string.IsNullOrWhiteSpace(barcode))
            missing.Add("баркод");
        if (string.IsNullOrWhiteSpace(catalogNumber))
            missing.Add("каталожный номер");
        return missing;
    }

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
            .FirstOrDefaultAsync(c => c.Id == meta.SenderCompanyId!.Value, ct)
            ?? throw new BusinessException("Компания-отправитель не найдена.", "COMPANY_NOT_FOUND");

        var receiver = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == meta.ReceiverCompanyId!.Value, ct)
            ?? throw new BusinessException("Компания-получатель не найдена.", "COMPANY_NOT_FOUND");

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
