using Application.Interfaces;
using Common.DTOs;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class CompanyService : ICompanyService
{
    private readonly SuspenseManagerDbContext _db;

    public CompanyService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<Company>> GetCompaniesAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Companies
            .AsNoTracking()
            .Where(c => c.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<Company?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.ArchiveLevel == 0, ct);
    }
}
