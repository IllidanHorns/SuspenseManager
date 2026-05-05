using Application.Helpers;
using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace Application.Services;

public class BackOfficeService : IBackOfficeService
{
    private readonly SuspenseManagerDbContext _db;
    private readonly IAuditService _audit;

    public BackOfficeService(SuspenseManagerDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Список и детали заданий
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<(List<BackOfficeTaskDto> Items, int TotalCount, int TotalPages)> GetTasksAsync(
        BackOfficeTasksRequest request, CancellationToken ct = default)
    {
        var query = _db.BackOfficeTasks
            .AsNoTracking()
            .Include(t => t.CreatedBy)
                .ThenInclude(a => a.User)
            .Include(t => t.Group)
                .ThenInclude(g => g.GroupMetaData)
            .Where(t => t.ArchiveLevel == 0 && t.TaskStatus == (int)BackOfficeTaskStatus.Open);

        if (request.TaskType.HasValue)
            query = query.Where(t => t.Group.BusinessStatus == request.TaskType.Value);

        var totalCount = await query.CountAsync(ct);
        var pageSize = request.PageSize;
        var pageNumber = request.PageNumber;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var tasks = await query
            .OrderByDescending(t => t.CreateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Загружаем кол-во суспенсов пакетом
        var groupIds = tasks.Select(t => t.GroupId).Distinct().ToList();
        var suspenseCounts = await _db.SuspenseLines
            .Where(s => groupIds.Contains(s.GroupId!.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        var items = tasks.Select(t =>
        {
            var meta = t.Group.GroupMetaData;
            return new BackOfficeTaskDto
            {
                Id = t.Id,
                GroupId = t.GroupId,
                GroupStatus = t.Group.BusinessStatus,
                SuspenseCount = suspenseCounts.GetValueOrDefault(t.GroupId, 0),
                ProblemDescription = t.ProblemDescription,
                TaskStatus = t.TaskStatus,
                CreateTime = t.CreateTime,
                CreatedByAccountId = t.CreatedByAccountId,
                CreatedByLogin = t.CreatedBy.Login,
                CreatedByName = t.CreatedBy.User != null
                    ? $"{t.CreatedBy.User.Name} {t.CreatedBy.User.Surname}".Trim()
                    : null,
                Artist = meta?.Artist,
                Title = meta?.Title,
                Isrc = meta?.Isrc,
                Barcode = meta?.Barcode,
            };
        }).ToList();

        return (items, totalCount, totalPages == 0 ? 1 : totalPages);
    }

    public async Task<BackOfficeTask> GetTaskAsync(int taskId, CancellationToken ct = default)
    {
        var task = await _db.BackOfficeTasks
            .Include(t => t.CreatedBy)
                .ThenInclude(a => a.User)
            .Include(t => t.Group)
                .ThenInclude(g => g.GroupMetaData)
            .Include(t => t.Group)
                .ThenInclude(g => g.GroupMetaRights)
            .Include(t => t.Group)
                .ThenInclude(g => g.CatalogProduct)
            .Include(t => t.Group)
                .ThenInclude(g => g.Account)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Задание #{taskId} не найдено");

        // Подсчёт суспенсов для группы
        task.Group.SuspenseCount = await _db.SuspenseLines
            .CountAsync(s => s.GroupId == task.GroupId && s.ArchiveLevel == 0, ct);

        return task;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // BO-специфичные действия
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<SuspenseGroup> ReturnGroupAsync(int taskId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        var returnStatus = group.BusinessStatus switch
        {
            (int)BusinessStatus.BackOfficeNoProduct => (int)BusinessStatus.InGroupNoProduct,
            (int)BusinessStatus.BackOfficeNoRights  => (int)BusinessStatus.InGroupNoRights,
            _ => throw new BusinessException(
                "Возврат возможен только для групп со статусом 120 или 320", "INVALID_STATUS")
        };

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = returnStatus;
        group.ChangeTime = DateTime.UtcNow;

        CloseTask(task, BackOfficeTaskStatus.ReturnedToOperator);
        await UpdateSuspenseLinesStatusAsync(group.Id, returnStatus, null, ct);

        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(group.Id, prevStatus, returnStatus, ct);
        await _audit.LogGroupLinesAsync(group.Id, prevStatus, returnStatus, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    public async Task DeleteGroupAsync(int taskId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        var now = DateTime.UtcNow;
        var prevStatus = group.BusinessStatus;

        // Аудит до архивирования — LogGroupLinesAsync ищет строки по ArchiveLevel=0
        await _audit.LogGroupAsync(group.Id, prevStatus, prevStatus, ct);
        await _audit.LogGroupLinesAsync(group.Id, prevStatus, prevStatus, ct);

        CloseTask(task, BackOfficeTaskStatus.GroupDeleted);

        group.ArchiveLevel = 1;
        group.ArchiveTime = now;
        group.ChangeTime = now;

        if (group.MetaDataId.HasValue)
        {
            var meta = await _db.GroupMetadata
                .FirstOrDefaultAsync(m => m.Id == group.MetaDataId.Value, ct);
            if (meta != null) { meta.ArchiveLevel = 1; meta.ArchiveTime = now; }
        }

        if (group.MetaRightsId.HasValue)
        {
            var rights = await _db.GroupMetaRights
                .FirstOrDefaultAsync(r => r.Id == group.MetaRightsId.Value, ct);
            if (rights != null) { rights.ArchiveLevel = 1; rights.ArchiveTime = now; }
        }

        var links = await _db.SuspenseGroupLinks
            .Where(l => l.SuspenseGroupId == group.Id && l.ArchiveLevel == 0)
            .ToListAsync(ct);
        foreach (var link in links) { link.ArchiveLevel = 1; link.ArchiveTime = now; }

        var suspenses = await _db.SuspenseLines
            .Where(s => s.GroupId == group.Id && s.ArchiveLevel == 0)
            .ToListAsync(ct);
        foreach (var s in suspenses)
        {
            s.ArchiveLevel = 1;
            s.ArchiveTime = now;
            s.ChangeTime = now;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<SuspenseGroup> LinkProductAsync(int taskId, int productId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        if (group.BusinessStatus != (int)BusinessStatus.BackOfficeNoProduct)
            throw new BusinessException(
                "Привязка продукта доступна только для заданий с типом 'нет продукта' (120)",
                "INVALID_STATUS");

        var product = await _db.CatalogProducts
            .Include(p => p.ProductType)
            .FirstOrDefaultAsync(p => p.Id == productId && p.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Продукт не найден", "PRODUCT_NOT_FOUND", 404);

        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == product.Id && r.ArchiveLevel == 0, ct);

        // Если прав нет — группа переходит в 320 (BO no-rights), задание остаётся открытым
        // Если права есть — группа сразу в 88, задание закрывается
        var newStatus = rightsExist
            ? (int)BusinessStatus.Validated
            : (int)BusinessStatus.BackOfficeNoRights;

        var meta = await GroupMetadataCatalogMapper.GetOrCreateForGroupAsync(_db, group.Id, ct);
        GroupMetadataCatalogMapper.ApplyFromCatalogProduct(meta, product);

        var prevStatus = group.BusinessStatus;
        group.CatalogProductId = product.Id;
        group.BusinessStatus = newStatus;
        group.ChangeTime = DateTime.UtcNow;

        if (newStatus == (int)BusinessStatus.Validated)
            CloseTask(task, BackOfficeTaskStatus.Completed);

        await UpdateSuspenseLinesStatusAsync(group.Id, newStatus, product.Id, ct);
        await _db.SaveChangesAsync(ct);

        if (group.MetaDataId == null)
        {
            group.MetaDataId = meta.Id;
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogGroupAsync(group.Id, prevStatus, newStatus, ct);
        await _audit.LogGroupLinesAsync(group.Id, prevStatus, newStatus, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    public async Task<SuspenseGroup> CompleteTaskAsync(int taskId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        if (group.BusinessStatus != (int)BusinessStatus.BackOfficeNoRights)
            throw new BusinessException(
                "Валидация доступна только для заданий с типом 'нет прав' (320)",
                "INVALID_STATUS");

        if (!group.CatalogProductId.HasValue)
            throw new BusinessException(
                "Группа не привязана к продукту каталога",
                "NO_PRODUCT");

        var rightsExist = await _db.CatalogProductRights
            .AnyAsync(r => r.CatalogProductId == group.CatalogProductId.Value && r.ArchiveLevel == 0, ct);

        if (!rightsExist)
        {
            // Права не найдены в каталоге — пытаемся создать из GroupMetaRights
            ValidateMetaRightsFields(group.GroupMetaRights);
            await CreateCatalogRightsFromMetaAsync(group.CatalogProductId.Value, group.GroupMetaRights!, ct);
        }

        var prevStatus = group.BusinessStatus;
        group.BusinessStatus = (int)BusinessStatus.Validated;
        group.ChangeTime = DateTime.UtcNow;

        CloseTask(task, BackOfficeTaskStatus.Completed);
        await UpdateSuspenseLinesStatusAsync(group.Id, (int)BusinessStatus.Validated, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(group.Id, prevStatus, (int)BusinessStatus.Validated, ct);
        await _audit.LogGroupLinesAsync(group.Id, prevStatus, (int)BusinessStatus.Validated, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    public async Task<PagedResponse<CatalogProduct>> GetPossibleProductsAsync(
        int taskId, PagedRequest request, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        if (group.BusinessStatus != (int)BusinessStatus.BackOfficeNoProduct)
            throw new BusinessException(
                "Поиск продуктов доступен только для заданий с типом 'нет продукта' (120)",
                "INVALID_STATUS");

        var meta = group.GroupMetaData;
        var firstSuspense = await _db.SuspenseLines
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GroupId == group.Id && s.ArchiveLevel == 0, ct);

        var isrc = meta?.Isrc ?? firstSuspense?.Isrc;
        var barcode = meta?.Barcode ?? firstSuspense?.Barcode;
        var title = meta?.Title ?? firstSuspense?.TrackTitle;
        var artist = meta?.Artist ?? firstSuspense?.Artist;

        var query = _db.CatalogProducts
            .AsNoTracking()
            .Where(p => p.ArchiveLevel == 0)
            .Where(p =>
                (!string.IsNullOrEmpty(isrc)   && p.Isrc == isrc) ||
                (!string.IsNullOrEmpty(barcode) && p.Barcode == barcode) ||
                (!string.IsNullOrEmpty(title)   && p.ProductName != null && p.ProductName.Contains(title)) ||
                (!string.IsNullOrEmpty(artist)  && p.Artist != null && p.Artist.Contains(artist)));

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<List<CatalogProductRights>> SearchCatalogRightsAsync(
        int taskId,
        string? artist,
        string? isrc,
        string? productName,
        string? barcode,
        string? rightsTerritoryCode,
        string? rightsDocNumber,
        bool combineProductFieldsWithAnd,
        CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        if (group.BusinessStatus != (int)BusinessStatus.BackOfficeNoRights)
            throw new BusinessException(
                "Поиск прав доступен только для заданий с типом 'нет прав' (320)",
                "INVALID_STATUS");

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

    public async Task<SuspenseGroup> CopyRightsToProductAsync(int taskId, int rightsId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var task = await GetTaskOrThrowAsync(taskId, ct);
        var group = task.Group;

        if (group.BusinessStatus != (int)BusinessStatus.BackOfficeNoRights)
            throw new BusinessException(
                "Копирование прав доступно только для заданий с типом 'нет прав' (320)",
                "INVALID_STATUS");

        if (!group.CatalogProductId.HasValue)
            throw new BusinessException("Группа не привязана к продукту каталога", "NO_PRODUCT");

        var source = await _db.CatalogProductRights
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rightsId && r.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Запись прав не найдена", "RIGHTS_NOT_FOUND", 404);

        // Не создаём дубль
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

        CloseTask(task, BackOfficeTaskStatus.Completed);
        await UpdateSuspenseLinesStatusAsync(group.Id, (int)BusinessStatus.Validated, null, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.LogGroupAsync(group.Id, prevStatus, (int)BusinessStatus.Validated, ct);
        await _audit.LogGroupLinesAsync(group.Id, prevStatus, (int)BusinessStatus.Validated, ct);
        await tx.CommitAsync(ct);

        return group;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<BackOfficeTask> GetTaskOrThrowAsync(int taskId, CancellationToken ct)
    {
        return await _db.BackOfficeTasks
            .Include(t => t.Group)
                .ThenInclude(g => g.GroupMetaData)
            .Include(t => t.Group)
                .ThenInclude(g => g.GroupMetaRights)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Задание #{taskId} не найдено или уже закрыто");
    }

    /// <summary>
    /// Помечает задание как закрытое (не вызывает SaveChanges — только трекает изменение).
    /// </summary>
    private static void CloseTask(BackOfficeTask task, BackOfficeTaskStatus reason)
    {
        task.TaskStatus = (int)reason;
        task.ArchiveLevel = 1;
        task.ArchiveTime = DateTime.UtcNow;
        task.ChangeTime = DateTime.UtcNow;
    }

    private async Task UpdateSuspenseLinesStatusAsync(
        int groupId, int newStatus, int? productId, CancellationToken ct)
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

    private static void ValidateMetaRightsFields(GroupMetaRights? meta)
    {
        if (meta == null)
            throw new BusinessException(
                "Метаправа группы не заполнены. Заполните права вручную или скопируйте из каталога.",
                "META_RIGHTS_EMPTY");

        var missing = new List<string>();
        if (!meta.SenderCompanyId.HasValue)               missing.Add("Компания-отправитель");
        if (!meta.ReceiverCompanyId.HasValue)              missing.Add("Компания-получатель");
        if (!meta.TerritoryId.HasValue)                    missing.Add("Территория");
        if (string.IsNullOrWhiteSpace(meta.TerritoryCode)) missing.Add("Код территории");
        if (!meta.DocStart.HasValue)                       missing.Add("Дата начала договора");
        if (!meta.DocEnd.HasValue)                         missing.Add("Дата окончания договора");
        if (!meta.Share.HasValue)                          missing.Add("Доля (%)");

        if (missing.Count > 0)
            throw new BusinessException(
                $"Не заполнены обязательные поля метаправ: {string.Join(", ", missing)}",
                "META_RIGHTS_INCOMPLETE");
    }

    private async Task CreateCatalogRightsFromMetaAsync(
        int productId, GroupMetaRights meta, CancellationToken ct)
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
