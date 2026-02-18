using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class SuspenseService : ISuspenseService
{
    private readonly SuspenseManagerDbContext _db;

    public SuspenseService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<SuspenseLine>> GetSuspensesAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.SuspenseLines
            .AsNoTracking()
            .Where(s => s.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<SuspenseLine?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SuspenseLines
            .AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.CatalogProduct)
            .Include(s => s.SenderCompanyR)
            .Include(s => s.RecipientCompanyR)
            .FirstOrDefaultAsync(s => s.Id == id && s.ArchiveLevel == 0, ct);
    }

    public async Task<SuspenseLine> UpdateAsync(int id, SuspenseLineDto dto, CancellationToken ct = default)
    {
        var entity = await _db.SuspenseLines
            .FirstOrDefaultAsync(s => s.Id == id && s.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Суспенс с ID {id} не найден");

        entity.Isrc = dto.Isrc ?? entity.Isrc;
        entity.Barcode = dto.Barcode ?? entity.Barcode;
        entity.CatalogNumber = dto.CatalogNumber ?? entity.CatalogNumber;
        entity.Artist = dto.Artist ?? entity.Artist;
        entity.TrackTitle = dto.TrackTitle ?? entity.TrackTitle;
        entity.Operator = dto.Operator ?? entity.Operator;
        entity.SenderCompany = dto.SenderCompany ?? entity.SenderCompany;
        entity.RecipientCompany = dto.RecipientCompany ?? entity.RecipientCompany;
        entity.AgreementType = dto.AgreementType ?? entity.AgreementType;
        entity.AgreementNumber = dto.AgreementNumber ?? entity.AgreementNumber;
        entity.TerritoryCode = dto.TerritoryCode ?? entity.TerritoryCode;
        entity.Genre = dto.Genre ?? entity.Genre;
        entity.SenderCompanyId = dto.SenderCompanyId ?? entity.SenderCompanyId;
        entity.RecipientCompanyId = dto.RecipientCompanyId ?? entity.RecipientCompanyId;
        entity.ChangeTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<PagedResponse<SuspenseLine>> GetUngroupedAsync(int businessStatus, PagedRequest request, CancellationToken ct = default)
    {
        if (businessStatus != 0 && businessStatus != 1)
        {
            throw new BusinessException("Допустимые статусы: 0 (нет продукта) или 1 (нет прав)", "INVALID_STATUS");
        }

        var query = _db.SuspenseLines
            .AsNoTracking()
            .Where(s => s.ArchiveLevel == 0 && s.BusinessStatus == businessStatus && s.GroupId == null);

        return await query.ToPagedResponseAsync(request, ct);
    }
}
