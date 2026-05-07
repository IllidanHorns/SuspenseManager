using Application.Interfaces;
using Common.DTOs;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class MonitorService : IMonitorService
{
    private readonly SuspenseManagerDbContext _db;

    // Статусы, которые считаются «живыми» (группа ещё обрабатывается)
    private static readonly int[] ActiveStatuses = [15, 16, 30, 32, 120, 320];

    // Пороги для флагов — применяются к активным (15/16) и отложенным (30/32) группам
    private const int ActiveWarningDays    =  7;
    private const int ActiveCriticalDays   = 14;
    private const int PostponedWarningDays  = 14;
    private const int PostponedCriticalDays = 30;

    public MonitorService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    // ── Сводка по всем операторам ───────────────────────────────────────────────

    public async Task<List<OperatorMonitorDto>> GetOperatorSummaryAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var rawGroups = await _db.SuspenseGroups
            .AsNoTracking()
            .Where(g => g.ArchiveLevel == 0 && ActiveStatuses.Contains(g.BusinessStatus))
            .Select(g => new
            {
                g.Id,
                g.AccountId,
                AccountLogin = g.Account.Login,
                UserSurname  = g.Account.User != null ? g.Account.User.Surname : null,
                UserName     = g.Account.User != null ? g.Account.User.Name    : null,
                g.BusinessStatus,
                g.CreateTime,
                g.ChangeTime,
                SuspenseCount = g.SuspenseLines.Count(s => s.ArchiveLevel == 0),
            })
            .ToListAsync(ct);

        if (rawGroups.Count == 0)
            return [];

        var result = rawGroups
            .GroupBy(g => g.AccountId)
            .Select(grp =>
            {
                var list        = grp.ToList();
                var flags       = list.Select(g => ComputeFlag(g.BusinessStatus, (int)(now - g.CreateTime).TotalDays, (int)(now - g.ChangeTime).TotalDays)).ToList();
                var first       = list[0];
                var fullName    = (first.UserSurname + " " + first.UserName).Trim();

                return new OperatorMonitorDto
                {
                    AccountId            = grp.Key,
                    Login                = first.AccountLogin,
                    FullName             = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                    ActiveGroupsCount    = list.Count(g => g.BusinessStatus is 15 or 16),
                    PostponedGroupsCount = list.Count(g => g.BusinessStatus is 30 or 32),
                    BackOfficeGroupsCount= list.Count(g => g.BusinessStatus is 120 or 320),
                    TotalSuspensesCount  = list.Sum(g => g.SuspenseCount),
                    OldestGroupAgeDays   = (int)list.Max(g => (now - g.CreateTime).TotalDays),
                    WarningGroupsCount   = flags.Count(f => f.Level == "warning"),
                    CriticalGroupsCount  = flags.Count(f => f.Level == "critical"),
                };
            })
            .OrderByDescending(o => o.CriticalGroupsCount)
            .ThenByDescending(o => o.WarningGroupsCount)
            .ThenByDescending(o => o.ActiveGroupsCount)
            .ThenBy(o => o.Login)
            .ToList();

        return result;
    }

    // ── Группы конкретного оператора ────────────────────────────────────────────

    public async Task<List<OperatorGroupDto>> GetOperatorGroupsAsync(int accountId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var groups = await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.GroupMetaData)
            .Include(g => g.CatalogProduct)
            .Where(g => g.AccountId == accountId && g.ArchiveLevel == 0 && ActiveStatuses.Contains(g.BusinessStatus))
            .ToListAsync(ct);

        if (groups.Count == 0)
            return [];

        var groupIds = groups.Select(g => g.Id).ToList();
        var suspenseCounts = await _db.SuspenseLines
            .Where(s => s.GroupId != null && groupIds.Contains(s.GroupId.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return groups
            .Select(g =>
            {
                var ageDays            = (int)(now - g.CreateTime).TotalDays;
                var daysSinceActivity  = (int)(now - g.ChangeTime).TotalDays;
                var (level, reason)   = ComputeFlag(g.BusinessStatus, ageDays, daysSinceActivity);

                return new OperatorGroupDto
                {
                    GroupId               = g.Id,
                    BusinessStatus        = g.BusinessStatus,
                    SuspenseCount         = suspenseCounts.GetValueOrDefault(g.Id),
                    AgeDays               = ageDays,
                    DaysSinceLastActivity = daysSinceActivity,
                    LastActivityTime      = g.ChangeTime.ToString("o"),
                    FlagLevel             = level,
                    FlagReason            = reason,
                    Artist = g.GroupMetaData?.Artist  ?? g.CatalogProduct?.Artist,
                    Title  = g.GroupMetaData?.Title   ?? g.CatalogProduct?.ProductName,
                    Isrc   = g.GroupMetaData?.Isrc    ?? g.CatalogProduct?.Isrc,
                };
            })
            .OrderByDescending(g => g.FlagLevel == "critical")
            .ThenByDescending(g => g.FlagLevel == "warning")
            .ThenByDescending(g => g.AgeDays)
            .ToList();
    }

    // ── Вычисление флага ────────────────────────────────────────────────────────

    private static (string Level, string? Reason) ComputeFlag(int status, int ageDays, int daysSinceActivity)
    {
        if (status is 15 or 16)
        {
            if (daysSinceActivity >= ActiveCriticalDays)
                return ("critical", $"Нет активности {daysSinceActivity} дн.");
            if (daysSinceActivity >= ActiveWarningDays)
                return ("warning",  $"Нет активности {daysSinceActivity} дн.");
        }
        else if (status is 30 or 32)
        {
            if (ageDays >= PostponedCriticalDays)
                return ("critical", $"Отложена {ageDays} дн.");
            if (ageDays >= PostponedWarningDays)
                return ("warning",  $"Отложена {ageDays} дн.");
        }
        return ("none", null);
    }
}
