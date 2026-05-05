using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
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
        var codeExists = await _db.Territories.AnyAsync(
            t => t.TerritoryCode == dto.TerritoryCode && t.ArchiveLevel == 0, ct);
        if (codeExists)
            throw new BusinessException("Территория с таким кодом уже существует", "TERRITORY_CODE_EXISTS", 409);

        if (!string.IsNullOrWhiteSpace(dto.TerritoryName))
        {
            var nameExists = await _db.Territories.AnyAsync(
                t => t.TerritoryName == dto.TerritoryName && t.ArchiveLevel == 0, ct);
            if (nameExists)
                throw new BusinessException("Территория с таким названием уже существует", "TERRITORY_NAME_EXISTS", 409);
        }

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

        if (dto.TerritoryCode != null)
        {
            var codeExists = await _db.Territories.AnyAsync(
                t => t.TerritoryCode == dto.TerritoryCode && t.Id != id && t.ArchiveLevel == 0, ct);
            if (codeExists)
                throw new BusinessException("Территория с таким кодом уже существует", "TERRITORY_CODE_EXISTS", 409);
            territory.TerritoryCode = dto.TerritoryCode;
        }
        if (dto.TerritoryName != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.TerritoryName))
            {
                var nameExists = await _db.Territories.AnyAsync(
                    t => t.TerritoryName == dto.TerritoryName && t.Id != id && t.ArchiveLevel == 0, ct);
                if (nameExists)
                    throw new BusinessException("Территория с таким названием уже существует", "TERRITORY_NAME_EXISTS", 409);
            }
            territory.TerritoryName = dto.TerritoryName;
        }

        territory.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return territory;
    }
}
