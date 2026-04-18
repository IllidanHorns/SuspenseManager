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

    public async Task<Territory> CreateAsync(CreateTerritoryDto dto, CancellationToken ct = default)
    {
        var territory = new Territory
        {
            TerritoryCode = dto.TerritoryCode,
            TerritoryName = dto.TerritoryName ?? string.Empty,
            CreateTime    = DateTime.UtcNow,
            ArchiveLevel  = 0,
        };
        _db.Territories.Add(territory);
        await _db.SaveChangesAsync(ct);
        return territory;
    }

    public async Task<Territory> UpdateAsync(int id, UpdateTerritoryDto dto, CancellationToken ct = default)
    {
        var territory = await _db.Territories
            .FirstOrDefaultAsync(t => t.Id == id && t.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Территория #{id} не найдена");

        if (dto.TerritoryCode != null) territory.TerritoryCode = dto.TerritoryCode;
        if (dto.TerritoryName != null) territory.TerritoryName = dto.TerritoryName;

        territory.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return territory;
    }
}
