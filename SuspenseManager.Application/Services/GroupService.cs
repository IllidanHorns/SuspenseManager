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

        return await query.ToPagedResponseAsync(request, ct);
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

        return await query.ToPagedResponseAsync(request, ct);
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

        return await query.ToPagedResponseAsync(request, ct);
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
