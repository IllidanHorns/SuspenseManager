using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class AuditService : IAuditService
{
    private readonly SuspenseManagerDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(SuspenseManagerDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    // ── Запись событий ──────────────────────────────────────────────────────

    public async Task LogGroupAsync(int groupId, int? statusFrom, int statusTo, CancellationToken ct = default)
    {
        var (id, login, name) = await GetCurrentUserContextAsync(ct);

        _db.SuspenseGroupLogs.Add(new SuspenseGroupLog
        {
            SuspenseGroupId = groupId,
            StatusFrom = statusFrom,
            StatusTo = statusTo,
            AccountId = id,
            AccountLogin = login,
            AccountName = name,
            OperationTime = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task LogGroupLinesAsync(int groupId, int? statusFrom, int statusTo, CancellationToken ct = default)
    {
        var (id, login, name) = await GetCurrentUserContextAsync(ct);
        var now = DateTime.UtcNow;

        var lineIds = await _db.SuspenseLines
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var lineId in lineIds)
        {
            _db.SuspenseLineLogs.Add(new SuspenseLineLog
            {
                SuspenseLineId = lineId,
                GroupId = groupId,
                StatusFrom = statusFrom,
                StatusTo = statusTo,
                AccountId = id,
                AccountLogin = login,
                AccountName = name,
                OperationTime = now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task LogLineAsync(int lineId, int? groupId, int? statusFrom, int statusTo, CancellationToken ct = default)
    {
        var (id, login, name) = await GetCurrentUserContextAsync(ct);

        _db.SuspenseLineLogs.Add(new SuspenseLineLog
        {
            SuspenseLineId = lineId,
            GroupId = groupId,
            StatusFrom = statusFrom,
            StatusTo = statusTo,
            AccountId = id,
            AccountLogin = login,
            AccountName = name,
            OperationTime = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    // ── Чтение аудита ───────────────────────────────────────────────────────

    public async Task<PagedResponse<AuditGroupDto>> GetGroupsAsync(
        AuditGroupsRequest request, int currentAccountId, CancellationToken ct = default)
    {
        // Справочник статусов для маппинга кодов → названия
        var statusDict = await _db.StatusDictionary
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Code, s => s.Name, ct);

        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
                .ThenInclude(a => a.User)
            .Include(g => g.CatalogProduct)
            .Where(g => g.ArchiveLevel == 0);

        if (request.Status.HasValue)
            query = query.Where(g => g.BusinessStatus == request.Status.Value);

        if (request.OnlyMine)
            query = query.Where(g => g.AccountId == currentAccountId);
        else if (request.CreatedByAccountId.HasValue)
            query = query.Where(g => g.AccountId == request.CreatedByAccountId.Value);

        if (request.CreatedFrom.HasValue)
            query = query.Where(g => g.CreateTime >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(g => g.CreateTime <= request.CreatedTo.Value);

        var totalCount = await query.CountAsync(ct);

        var groups = await query
            .OrderByDescending(g => g.CreateTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.Id).ToList();

        // Последний лог для каждой группы
        var lastLogs = await _db.SuspenseGroupLogs
            .AsNoTracking()
            .Where(l => groupIds.Contains(l.SuspenseGroupId))
            .GroupBy(l => l.SuspenseGroupId)
            .Select(g => g.OrderByDescending(l => l.OperationTime).First())
            .ToListAsync(ct);

        var lastLogByGroup = lastLogs.ToDictionary(l => l.SuspenseGroupId);

        // Фильтр по дате последнего изменения (постфильтрация в памяти)
        var items = groups
            .Select(g =>
            {
                lastLogByGroup.TryGetValue(g.Id, out var lastLog);

                var lastChangeTime = lastLog?.OperationTime;
                if (request.LastChangedFrom.HasValue && (lastChangeTime == null || lastChangeTime < request.LastChangedFrom.Value))
                    return null;
                if (request.LastChangedTo.HasValue && (lastChangeTime == null || lastChangeTime > request.LastChangedTo.Value))
                    return null;

                statusDict.TryGetValue(g.BusinessStatus, out var statusName);
                string? lastStatusFromName = null;
                string? lastStatusToName = null;
                if (lastLog?.StatusFrom.HasValue == true)
                    statusDict.TryGetValue(lastLog.StatusFrom.Value, out lastStatusFromName);
                if (lastLog != null)
                    statusDict.TryGetValue(lastLog.StatusTo, out lastStatusToName);

                return new AuditGroupDto
                {
                    Id = g.Id,
                    BusinessStatus = g.BusinessStatus,
                    StatusName = statusName,
                    CreateTime = g.CreateTime,
                    CreatedByAccountId = g.AccountId,
                    CreatedByLogin = g.Account.Login,
                    CreatedByName = g.Account.User != null
                        ? $"{g.Account.User.Name} {g.Account.User.Surname}".Trim()
                        : null,
                    LastChangeTime = lastLog?.OperationTime,
                    LastStatusFrom = lastLog?.StatusFrom,
                    LastStatusFromName = lastStatusFromName,
                    LastStatusTo = lastLog?.StatusTo,
                    LastStatusToName = lastStatusToName,
                    LastChangedByLogin = lastLog?.AccountLogin,
                    LastChangedByName = lastLog?.AccountName,
                    CatalogProductId = g.CatalogProductId,
                    ProductName = g.CatalogProduct?.ProductName
                };
            })
            .Where(x => x != null)
            .Cast<AuditGroupDto>()
            .ToList();

        return new PagedResponse<AuditGroupDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResponse<AuditLogEntryDto>> GetGroupLogsAsync(
        int groupId, PagedRequest request, CancellationToken ct = default)
    {
        var statusDict = await _db.StatusDictionary
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Code, s => s.Name, ct);

        var query = _db.SuspenseGroupLogs
            .AsNoTracking()
            .Where(l => l.SuspenseGroupId == groupId);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.OperationTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = logs.Select(l =>
        {
            statusDict.TryGetValue(l.StatusTo, out var statusToName);
            string? statusFromName = null;
            if (l.StatusFrom.HasValue)
                statusDict.TryGetValue(l.StatusFrom.Value, out statusFromName);

            return new AuditLogEntryDto
            {
                Id = l.Id,
                StatusFrom = l.StatusFrom,
                StatusFromName = statusFromName,
                StatusTo = l.StatusTo,
                StatusToName = statusToName,
                AccountId = l.AccountId,
                AccountLogin = l.AccountLogin,
                AccountName = l.AccountName,
                OperationTime = l.OperationTime
            };
        }).ToList();

        return new PagedResponse<AuditLogEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResponse<AuditLineDto>> GetLinesAsync(
        AuditLinesRequest request, int currentAccountId, CancellationToken ct = default)
    {
        var statusDict = await _db.StatusDictionary
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Code, s => s.Name, ct);

        // Только строки, которые сейчас в активной группе
        var query = _db.SuspenseLines
            .AsNoTracking()
            .Include(s => s.Group)
                .ThenInclude(g => g!.Account)
                    .ThenInclude(a => a.User)
            .Where(s => s.GroupId != null
                        && s.ArchiveLevel == 0
                        && s.Group!.ArchiveLevel == 0);

        if (request.Status.HasValue)
            query = query.Where(s => s.BusinessStatus == request.Status.Value);

        if (request.GroupId.HasValue)
            query = query.Where(s => s.GroupId == request.GroupId.Value);

        if (request.OnlyMine)
            query = query.Where(s => s.Group!.AccountId == currentAccountId);

        var totalCount = await query.CountAsync(ct);

        var lines = await query
            .OrderByDescending(s => s.ChangeTime ?? s.CreateTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var lineIds = lines.Select(s => s.Id).ToList();

        // Последний лог для каждой строки
        var lastLogs = await _db.SuspenseLineLogs
            .AsNoTracking()
            .Where(l => lineIds.Contains(l.SuspenseLineId))
            .GroupBy(l => l.SuspenseLineId)
            .Select(g => g.OrderByDescending(l => l.OperationTime).First())
            .ToListAsync(ct);

        var lastLogByLine = lastLogs.ToDictionary(l => l.SuspenseLineId);

        var items = lines
            .Select(s =>
            {
                lastLogByLine.TryGetValue(s.Id, out var lastLog);

                var lastChangeTime = lastLog?.OperationTime;
                if (request.LastChangedFrom.HasValue && (lastChangeTime == null || lastChangeTime < request.LastChangedFrom.Value))
                    return null;
                if (request.LastChangedTo.HasValue && (lastChangeTime == null || lastChangeTime > request.LastChangedTo.Value))
                    return null;

                statusDict.TryGetValue(s.BusinessStatus, out var statusName);
                string? lastStatusFromName = null;
                string? lastStatusToName = null;
                if (lastLog?.StatusFrom.HasValue == true)
                    statusDict.TryGetValue(lastLog.StatusFrom.Value, out lastStatusFromName);
                if (lastLog != null)
                    statusDict.TryGetValue(lastLog.StatusTo, out lastStatusToName);

                return new AuditLineDto
                {
                    Id = s.Id,
                    BusinessStatus = s.BusinessStatus,
                    StatusName = statusName,
                    GroupId = s.GroupId,
                    Isrc = s.Isrc,
                    Barcode = s.Barcode,
                    Artist = s.Artist,
                    TrackTitle = s.TrackTitle,
                    Operator = s.Operator,
                    CreateTime = s.CreateTime,
                    GroupCreatedByAccountId = s.Group?.AccountId,
                    GroupCreatedByLogin = s.Group?.Account?.Login,
                    LastChangeTime = lastLog?.OperationTime,
                    LastStatusFrom = lastLog?.StatusFrom,
                    LastStatusFromName = lastStatusFromName,
                    LastStatusTo = lastLog?.StatusTo,
                    LastStatusToName = lastStatusToName,
                    LastChangedByLogin = lastLog?.AccountLogin,
                    LastChangedByName = lastLog?.AccountName
                };
            })
            .Where(x => x != null)
            .Cast<AuditLineDto>()
            .ToList();

        return new PagedResponse<AuditLineDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResponse<AuditLogEntryDto>> GetLineLogsAsync(
        int lineId, PagedRequest request, CancellationToken ct = default)
    {
        var statusDict = await _db.StatusDictionary
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Code, s => s.Name, ct);

        var query = _db.SuspenseLineLogs
            .AsNoTracking()
            .Where(l => l.SuspenseLineId == lineId);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.OperationTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = logs.Select(l =>
        {
            statusDict.TryGetValue(l.StatusTo, out var statusToName);
            string? statusFromName = null;
            if (l.StatusFrom.HasValue)
                statusDict.TryGetValue(l.StatusFrom.Value, out statusFromName);

            return new AuditLogEntryDto
            {
                Id = l.Id,
                StatusFrom = l.StatusFrom,
                StatusFromName = statusFromName,
                StatusTo = l.StatusTo,
                StatusToName = statusToName,
                AccountId = l.AccountId,
                AccountLogin = l.AccountLogin,
                AccountName = l.AccountName,
                OperationTime = l.OperationTime
            };
        }).ToList();

        return new PagedResponse<AuditLogEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    // ── Вспомогательные методы ───────────────────────────────────────���───────

    private async Task<(int Id, string Login, string? Name)> GetCurrentUserContextAsync(CancellationToken ct)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return (0, "system", null);

        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("account_id");
        var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
        var login = user.FindFirstValue(ClaimTypes.Name) ?? "unknown";

        string? name = null;
        if (id > 0)
        {
            name = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => a.User != null
                    ? (a.User.Name + " " + a.User.Surname).Trim()
                    : (string?)null)
                .FirstOrDefaultAsync(ct);
        }

        return (id, login, name);
    }
}
