using Application.Interfaces;
using Common.DTOs;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class TerritoryService : ITerritoryService
{
    private readonly SuspenseManagerDbContext _db;

    public TerritoryService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<Territory>> GetTerritoriesAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Territories
            .AsNoTracking()
            .Where(t => t.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }
}
