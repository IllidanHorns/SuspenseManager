using Application.Interfaces;
using Common.DTOs;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Enums;

namespace Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly SuspenseManagerDbContext _db;

    public AnalyticsService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
    {
        var suspenses = _db.SuspenseLines.Where(s => s.ArchiveLevel == 0);

        if (dateFrom.HasValue)
            suspenses = suspenses.Where(s => s.CreateTime >= dateFrom.Value);
        if (dateTo.HasValue)
            suspenses = suspenses.Where(s => s.CreateTime < dateTo.Value.AddDays(1));

        var totalSuspenses   = await suspenses.CountAsync(ct);
        var noProduct        = await suspenses.CountAsync(s => s.BusinessStatus == (int)BusinessStatus.NoProduct, ct);
        var noRights         = await suspenses.CountAsync(s => s.BusinessStatus == (int)BusinessStatus.NoRights, ct);
        var inGroupNoProduct = await suspenses.CountAsync(s => s.BusinessStatus == (int)BusinessStatus.InGroupNoProduct, ct);
        var inGroupNoRights  = await suspenses.CountAsync(s => s.BusinessStatus == (int)BusinessStatus.InGroupNoRights, ct);
        var validated        = await suspenses.CountAsync(s => s.BusinessStatus == (int)BusinessStatus.Validated, ct);
        var backOffice       = await suspenses.CountAsync(s =>
            s.BusinessStatus == (int)BusinessStatus.BackOfficeNoProduct ||
            s.BusinessStatus == (int)BusinessStatus.BackOfficeNoRights, ct);
        var postponed        = await suspenses.CountAsync(s =>
            s.BusinessStatus == (int)BusinessStatus.PostponedNoProduct ||
            s.BusinessStatus == (int)BusinessStatus.PostponedNoRights, ct);

        var totalGroups    = await _db.SuspenseGroups.CountAsync(g => g.ArchiveLevel == 0, ct);
        var totalProducts  = await _db.CatalogProducts.CountAsync(p => p.ArchiveLevel == 0, ct);
        var totalCompanies = await _db.Companies.CountAsync(c => c.ArchiveLevel == 0, ct);

        var totalRevenue = await suspenses
            .SumAsync(s => s.Qty * (decimal)(s.Ppd ?? 0) * s.ExchangeRate, ct);
        var totalStreams = await suspenses.SumAsync(s => (long)s.Qty, ct);

        var topOperators = await suspenses
            .Where(s => s.Operator != null)
            .GroupBy(s => s.Operator)
            .Select(g => new OperatorStatDto
            {
                Operator = g.Key!,
                Count    = g.Count(),
                Revenue  = g.Sum(s => s.Qty * (decimal)(s.Ppd ?? 0) * s.ExchangeRate),
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var statusDistribution = await suspenses
            .GroupBy(s => s.BusinessStatus)
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var topTerritories = await suspenses
            .Where(s => s.TerritoryCode != null && s.TerritoryCode != "")
            .GroupBy(s => s.TerritoryCode)
            .Select(g => new TerritoryStatDto
            {
                TerritoryCode = g.Key!,
                Count   = g.Count(),
                Revenue = g.Sum(s => s.Qty * (decimal)(s.Ppd ?? 0) * s.ExchangeRate),
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var topCompanies = await suspenses
            .Where(s => s.SenderCompany != null && s.SenderCompany != "")
            .GroupBy(s => s.SenderCompany)
            .Select(g => new CompanyStatDto
            {
                CompanyName = g.Key!,
                Count = g.Count(),
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        return new DashboardDto
        {
            TotalSuspenses     = totalSuspenses,
            NoProductCount     = noProduct,
            NoRightsCount      = noRights,
            InGroupNoProduct   = inGroupNoProduct,
            InGroupNoRights    = inGroupNoRights,
            ValidatedCount     = validated,
            BackOfficeCount    = backOffice,
            PostponedCount     = postponed,
            TotalGroups        = totalGroups,
            TotalProducts      = totalProducts,
            TotalCompanies     = totalCompanies,
            TotalRevenue       = totalRevenue,
            TotalStreams        = totalStreams,
            TopOperators       = topOperators,
            StatusDistribution = statusDistribution,
            TopTerritories     = topTerritories,
            TopCompanies       = topCompanies,
        };
    }
}
