using Application.Interfaces;
using Common.DTOs;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace Application.Services;

public class GroupService : IGroupService
{
    private readonly SuspenseManagerDbContext _db;

    public GroupService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetNoProductGroupsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Where(g => g.ArchiveLevel == 0 && g.BusinessStatus == (int)BusinessStatus.InGroupNoProduct);

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillSuspenseCountsAsync(result.Items, ct);
        return result;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetNoRightsGroupsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Include(g => g.CatalogProduct)
            .Where(g => g.ArchiveLevel == 0 && g.BusinessStatus == (int)BusinessStatus.InGroupNoRights);

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillSuspenseCountsAsync(result.Items, ct);
        return result;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetSavedGroupsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Include(g => g.CatalogProduct)
            .Where(g => g.ArchiveLevel == 0 &&
                (g.BusinessStatus == (int)BusinessStatus.InGroupNoProduct ||
                 g.BusinessStatus == (int)BusinessStatus.InGroupNoRights));

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillSuspenseCountsAsync(result.Items, ct);
        return result;
    }

    public async Task<SuspenseGroup?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Include(g => g.CatalogProduct)
            .Include(g => g.SuspenseLines)
            .FirstOrDefaultAsync(g => g.Id == id && g.ArchiveLevel == 0, ct);
    }

    private async Task FillSuspenseCountsAsync(List<SuspenseGroup> groups, CancellationToken ct)
    {
        if (groups.Count == 0) return;

        var ids = groups.Select(g => g.Id).ToList();

        var counts = await _db.SuspenseLines
            .Where(s => s.GroupId.HasValue && ids.Contains(s.GroupId.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        foreach (var group in groups)
            group.SuspenseCount = counts.GetValueOrDefault(group.Id, 0);
    }

    public async Task<PagedResponse<SuspenseLine>> GetGroupSuspensesAsync(int groupId, PagedRequest request, CancellationToken ct = default)
    {
        var group = await _db.SuspenseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

        var query = _db.SuspenseLines
            .AsNoTracking()
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }
}
