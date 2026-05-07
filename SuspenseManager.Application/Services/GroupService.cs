using System.Globalization;
using Application.Helpers;
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

    public async Task<PagedResponse<SuspenseGroup>> GetNoProductGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Where(g => g.ArchiveLevel == 0 && g.BusinessStatus == (int)BusinessStatus.InGroupNoProduct);

        query = ApplyOnlyMineFilter(query, request, currentAccountId);
        query = ApplyMetadataFilters(query, request);
        query = ApplyAggregateRangeFilters(query, request);

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillGroupAggregatesAsync(result.Items, ct);
        return result;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetNoRightsGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default)
    {
        var query = _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Include(g => g.GroupMetaRights)
            .Include(g => g.CatalogProduct)
            .Where(g => g.ArchiveLevel == 0 && g.BusinessStatus == (int)BusinessStatus.InGroupNoRights);

        query = ApplyOnlyMineFilter(query, request, currentAccountId);
        query = ApplyMetadataOrCatalogFilters(query, request);
        query = ApplyAggregateRangeFilters(query, request);

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillGroupAggregatesAsync(result.Items, ct);
        return result;
    }

    public async Task<PagedResponse<SuspenseGroup>> GetSavedGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default)
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

        query = ApplyOnlyMineFilter(query, request, currentAccountId);
        query = ApplyMetadataOrCatalogFilters(query, request);
        query = ApplyAggregateRangeFilters(query, request);

        var result = await query.ToPagedResponseAsync(request, ct);
        await FillGroupAggregatesAsync(result.Items, ct);
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
            .FirstOrDefaultAsync(g => g.Id == id && g.ArchiveLevel == 0, ct);
    }

    public async Task<PagedResponse<SuspenseLine>> GetGroupSuspensesAsync(int groupId, PagedRequest request, CancellationToken ct = default)
    {
        var group = await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.GroupMetaData)
            .Include(g => g.CatalogProduct)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Группа с ID {groupId} не найдена");

        var query = _db.SuspenseLines
            .AsNoTracking()
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0);

        query = ApplyGroupSuspenseRevenueFilters(query, request.Filters, out var filtersWithoutRevenue);

        var sortBy = request.SortBy;
        var sortDir = request.SortDirection;
        if (string.Equals(sortBy, "Revenue", StringComparison.OrdinalIgnoreCase))
        {
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = desc
                ? query.OrderByDescending(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate)
                : query.OrderBy(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate);
            sortBy = null;
        }

        var effectiveRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = sortBy,
            SortDirection = string.IsNullOrWhiteSpace(sortDir) ? "asc" : sortDir,
            Filters = filtersWithoutRevenue
        };

        var result = await query.ToPagedResponseAsync(effectiveRequest, ct);
        foreach (var line in result.Items)
        {
            GroupMetadataSuspenseDisplay.ApplyToSuspenseLine(
                line, group.GroupMetaData, group.CatalogProduct, group.BusinessStatus);
        }

        return result;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static IQueryable<SuspenseGroup> ApplyOnlyMineFilter(
        IQueryable<SuspenseGroup> query, GroupListRequest request, int currentAccountId)
    {
        if (request.OnlyMine && currentAccountId > 0)
        {
            return query.Where(g => g.AccountId == currentAccountId);
        }

        return query;
    }

    // Filters on GroupMetaData only (for no-product groups)
    private static IQueryable<SuspenseGroup> ApplyMetadataFilters(IQueryable<SuspenseGroup> q, GroupListRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.Artist))
            q = q.Where(g => g.GroupMetaData != null && g.GroupMetaData.Artist != null &&
                              g.GroupMetaData.Artist.Contains(r.Artist));

        if (!string.IsNullOrWhiteSpace(r.Title))
            q = q.Where(g => g.GroupMetaData != null && g.GroupMetaData.Title != null &&
                              g.GroupMetaData.Title.Contains(r.Title));

        if (!string.IsNullOrWhiteSpace(r.Isrc))
            q = q.Where(g => g.GroupMetaData != null && g.GroupMetaData.Isrc != null &&
                              g.GroupMetaData.Isrc.Contains(r.Isrc));

        if (!string.IsNullOrWhiteSpace(r.Barcode))
            q = q.Where(g => g.GroupMetaData != null && g.GroupMetaData.Barcode != null &&
                              g.GroupMetaData.Barcode.Contains(r.Barcode));

        return q;
    }

    // Filters on GroupMetaData OR CatalogProduct (for no-rights and mixed lists)
    private static IQueryable<SuspenseGroup> ApplyMetadataOrCatalogFilters(IQueryable<SuspenseGroup> q, GroupListRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.Artist))
        {
            var v = r.Artist;
            q = q.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.Artist != null && g.CatalogProduct.Artist.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Artist != null && g.GroupMetaData.Artist.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(r.Title))
        {
            var v = r.Title;
            q = q.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.ProductName != null && g.CatalogProduct.ProductName.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Title != null && g.GroupMetaData.Title.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(r.Isrc))
        {
            var v = r.Isrc;
            q = q.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.Isrc != null && g.CatalogProduct.Isrc.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Isrc != null && g.GroupMetaData.Isrc.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(r.Barcode))
        {
            var v = r.Barcode;
            q = q.Where(g =>
                (g.CatalogProduct != null && g.CatalogProduct.Barcode != null && g.CatalogProduct.Barcode.Contains(v)) ||
                (g.GroupMetaData != null && g.GroupMetaData.Barcode != null && g.GroupMetaData.Barcode.Contains(v)));
        }

        if (!string.IsNullOrWhiteSpace(r.TerritoryCode))
        {
            var v = r.TerritoryCode;
            q = q.Where(g => g.GroupMetaRights != null && g.GroupMetaRights.TerritoryCode != null &&
                              g.GroupMetaRights.TerritoryCode.Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(r.DocNumber))
        {
            var v = r.DocNumber;
            q = q.Where(g => g.GroupMetaRights != null && g.GroupMetaRights.DocNumber != null &&
                              g.GroupMetaRights.DocNumber.Contains(v));
        }

        return q;
    }

    private IQueryable<SuspenseGroup> ApplyAggregateRangeFilters(IQueryable<SuspenseGroup> q, GroupListRequest r)
    {
        if (r.CountMin.HasValue)
        {
            int min = r.CountMin.Value;
            q = q.Where(g => _db.SuspenseLines.Count(s => s.GroupId == g.Id && s.ArchiveLevel == 0) >= min);
        }

        if (r.CountMax.HasValue)
        {
            int max = r.CountMax.Value;
            q = q.Where(g => _db.SuspenseLines.Count(s => s.GroupId == g.Id && s.ArchiveLevel == 0) <= max);
        }

        if (r.RevenueMin.HasValue)
        {
            double min = (double)r.RevenueMin.Value;
            q = q.Where(g => _db.SuspenseLines
                .Where(s => s.GroupId == g.Id && s.ArchiveLevel == 0)
                .Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate) >= min);
        }

        if (r.RevenueMax.HasValue)
        {
            double max = (double)r.RevenueMax.Value;
            q = q.Where(g => _db.SuspenseLines
                .Where(s => s.GroupId == g.Id && s.ArchiveLevel == 0)
                .Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate) <= max);
        }

        return q;
    }

    /// <summary>
    /// Фильтры по выручке строки (Qty × Ppd × курс) — не свойство модели, обрабатываем до ApplyFilters.
    /// Ключи: Revenue_gte, Revenue_lte, Revenue_gt, Revenue_lt (значение — число, инвариантная культура).
    /// </summary>
    private static IQueryable<SuspenseLine> ApplyGroupSuspenseRevenueFilters(
        IQueryable<SuspenseLine> query,
        Dictionary<string, string>? filters,
        out Dictionary<string, string>? rest)
    {
        rest = filters;
        if (filters == null || filters.Count == 0) return query;

        var copy = new Dictionary<string, string>();
        foreach (var kv in filters)
        {
            if (!kv.Key.StartsWith("Revenue", StringComparison.OrdinalIgnoreCase))
            {
                copy[kv.Key] = kv.Value;
                continue;
            }

            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            if (!double.TryParse(kv.Value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var num)) continue;

            if (kv.Key.Equals("Revenue_gte", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate >= num);
            else if (kv.Key.Equals("Revenue_lte", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate <= num);
            else if (kv.Key.Equals("Revenue_gt", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate > num);
            else if (kv.Key.Equals("Revenue_lt", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate < num);
        }

        rest = copy.Count > 0 ? copy : null;
        return query;
    }

    private async Task FillGroupAggregatesAsync(List<SuspenseGroup> groups, CancellationToken ct)
    {
        if (groups.Count == 0) return;

        var ids = groups.Select(g => g.Id).ToList();

        var aggs = await _db.SuspenseLines
            .Where(s => s.GroupId.HasValue && ids.Contains(s.GroupId.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId!.Value)
            .Select(g => new
            {
                GroupId = g.Key,
                Count = g.Count(),
                RevenueDouble = g.Sum(s => (double)s.Qty * (s.Ppd ?? 0.0) * (double)s.ExchangeRate)
            })
            .ToDictionaryAsync(x => x.GroupId, ct);

        foreach (var group in groups)
        {
            if (aggs.TryGetValue(group.Id, out var agg))
            {
                group.SuspenseCount = agg.Count;
                group.RevenueRub = (decimal)agg.RevenueDouble;
            }
            else
            {
                group.SuspenseCount = 0;
                group.RevenueRub = 0;
            }
        }
    }
}
