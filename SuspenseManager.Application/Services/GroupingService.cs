using Application.Helpers;
using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace Application.Services;

public class GroupingService : IGroupingService
{
    private readonly SuspenseManagerDbContext _db;

    public GroupingService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<GroupingPreviewItem>> PreviewAsync(
        GroupingPreviewRequest request, CancellationToken ct = default)
    {
        GroupingSqlBuilder.ValidateRequest(request.BusinessStatus, request.GroupByColumns);

        var offset = (request.PageNumber - 1) * request.PageSize;

        var (sql, countSql, parameters) = GroupingSqlBuilder.BuildPreviewSql(
            request.BusinessStatus,
            request.GroupByColumns,
            request.Filters,
            request.SortBy,
            request.SortDirection,
            offset,
            request.PageSize);

        // Нужны отдельные параметры для count, т.к. SqlParameter нельзя использовать повторно
        var countParams = CloneParameters(parameters
            .Where(p => p.ParameterName != "@pOffset" && p.ParameterName != "@pPageSize")
            .ToList());

        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        // COUNT-запрос (количество групп)
        int totalCount;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = countSql;
            countCmd.Parameters.AddRange(countParams.ToArray());
            var countResult = await countCmd.ExecuteScalarAsync(ct);
            totalCount = Convert.ToInt32(countResult);
        }

        // Основной запрос с данными
        var items = new List<GroupingPreviewItem>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var item = new GroupingPreviewItem();

                for (var i = 0; i < request.GroupByColumns.Count; i++)
                {
                    var colName = request.GroupByColumns[i];
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
                    item.Key[colName] = value;
                }

                item.Count = reader.GetInt32(request.GroupByColumns.Count);
                items.Add(item);
            }
        }

        return new PagedResponse<GroupingPreviewItem>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<SuspenseGroup> CommitAsync(
        GroupingCommitRequest request, CancellationToken ct = default)
    {
        GroupingSqlBuilder.ValidateRequest(request.BusinessStatus, request.GroupByColumns);

        var newStatus = request.BusinessStatus == 0
            ? (int)BusinessStatus.InGroupNoProduct
            : (int)BusinessStatus.InGroupNoRights;

        // Находим подходящие суспенсы
        var suspenses = await FindMatchingSuspensesAsync(request, ct);

        if (suspenses.Count == 0)
            throw new BusinessException(
                "Не найдено суспенсов, соответствующих критериям группировки",
                "NO_MATCHING_SUSPENSES");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Определяем CatalogProductId для статуса 1
            int? catalogProductId = null;
            if (request.BusinessStatus == 1)
            {
                catalogProductId = suspenses[0].ProductId;
            }

            // Создаём группу
            var group = new SuspenseGroup
            {
                BusinessStatus = newStatus,
                AccountId = request.AccountId,
                CatalogProductId = catalogProductId,
                CreateTime = DateTime.UtcNow,
                ChangeTime = DateTime.UtcNow,
                ArchiveLevel = 0
            };

            _db.SuspenseGroups.Add(group);
            await _db.SaveChangesAsync(ct);

            // Обновляем суспенсы и создаём связи
            var now = DateTime.UtcNow;
            foreach (var suspense in suspenses)
            {
                suspense.GroupId = group.Id;
                suspense.BusinessStatus = newStatus;
                suspense.ChangeTime = now;

                _db.SuspenseGroupLinks.Add(new SuspenseGroupLink
                {
                    SuspenseId = suspense.Id,
                    SuspenseGroupId = group.Id,
                    AccountId = request.AccountId,
                    BusinessStatus = newStatus,
                    CreateTime = now,
                    ChangeTime = now,
                    ArchiveLevel = 0
                });
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return group;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<List<SuspenseLine>> FindMatchingSuspensesAsync(
        GroupingCommitRequest request, CancellationToken ct)
    {
        if (request.BusinessStatus == 0)
        {
            return await FindNoProductSuspensesAsync(request, ct);
        }

        return await FindNoRightsSuspensesAsync(request, ct);
    }

    private async Task<List<SuspenseLine>> FindNoProductSuspensesAsync(
        GroupingCommitRequest request, CancellationToken ct)
    {
        // Для статуса 0 — все условия по полям SuspenseLine
        var query = _db.SuspenseLines
            .Where(s => s.BusinessStatus == (int)BusinessStatus.NoProduct
                        && s.ArchiveLevel == 0
                        && s.GroupId == null);

        query = ApplyKeyFilters(query, request.GroupByColumns, request.KeyValues);

        return await query.ToListAsync(ct);
    }

    private async Task<List<SuspenseLine>> FindNoRightsSuspensesAsync(
        GroupingCommitRequest request, CancellationToken ct)
    {
        // Для статуса 1 — часть условий по SuspenseLine, часть по CatalogProduct
        var suspenseColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProductId", "SenderCompany", "RecipientCompany", "Operator",
            "AgreementType", "AgreementNumber", "TerritoryCode"
        };

        var catalogColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Isrc", "Barcode", "CatalogNumber", "ProductName", "Artist"
        };

        var query = _db.SuspenseLines
            .Include(s => s.CatalogProduct)
            .Where(s => s.BusinessStatus == (int)BusinessStatus.NoRights
                        && s.ArchiveLevel == 0
                        && s.GroupId == null
                        && s.ProductId != null);

        // Фильтры по полям SuspenseLine
        var suspenseKeys = request.GroupByColumns
            .Where(c => suspenseColumns.Contains(c))
            .ToList();
        query = ApplyKeyFilters(query, suspenseKeys, request.KeyValues);

        // Фильтры по полям CatalogProduct
        foreach (var col in request.GroupByColumns.Where(c => catalogColumns.Contains(c)))
        {
            if (!request.KeyValues.TryGetValue(col, out var value))
                continue;

            query = col switch
            {
                "Isrc" => value == null
                    ? query.Where(s => s.CatalogProduct!.Isrc == null)
                    : query.Where(s => s.CatalogProduct!.Isrc == value),
                "Barcode" => value == null
                    ? query.Where(s => s.CatalogProduct!.Barcode == null)
                    : query.Where(s => s.CatalogProduct!.Barcode == value),
                "CatalogNumber" => value == null
                    ? query.Where(s => s.CatalogProduct!.CatalogNumber == null)
                    : query.Where(s => s.CatalogProduct!.CatalogNumber == value),
                "ProductName" => value == null
                    ? query.Where(s => s.CatalogProduct!.ProductName == null)
                    : query.Where(s => s.CatalogProduct!.ProductName == value),
                "Artist" => value == null
                    ? query.Where(s => s.CatalogProduct!.Artist == null)
                    : query.Where(s => s.CatalogProduct!.Artist == value),
                _ => query
            };
        }

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Применяет фильтры по полям SuspenseLine из KeyValues
    /// </summary>
    private static IQueryable<SuspenseLine> ApplyKeyFilters(
        IQueryable<SuspenseLine> query,
        List<string> columns,
        Dictionary<string, string?> keyValues)
    {
        foreach (var col in columns)
        {
            if (!keyValues.TryGetValue(col, out var value))
                continue;

            query = col switch
            {
                "Isrc" => value == null ? query.Where(s => s.Isrc == null) : query.Where(s => s.Isrc == value),
                "Barcode" => value == null ? query.Where(s => s.Barcode == null) : query.Where(s => s.Barcode == value),
                "CatalogNumber" => value == null ? query.Where(s => s.CatalogNumber == null) : query.Where(s => s.CatalogNumber == value),
                "Artist" => value == null ? query.Where(s => s.Artist == null) : query.Where(s => s.Artist == value),
                "TrackTitle" => value == null ? query.Where(s => s.TrackTitle == null) : query.Where(s => s.TrackTitle == value),
                "Genre" => value == null ? query.Where(s => s.Genre == null) : query.Where(s => s.Genre == value),
                "SenderCompany" => value == null ? query.Where(s => s.SenderCompany == null) : query.Where(s => s.SenderCompany == value),
                "RecipientCompany" => value == null ? query.Where(s => s.RecipientCompany == null) : query.Where(s => s.RecipientCompany == value),
                "Operator" => value == null ? query.Where(s => s.Operator == null) : query.Where(s => s.Operator == value),
                "AgreementType" => value == null ? query.Where(s => s.AgreementType == null) : query.Where(s => s.AgreementType == value),
                "AgreementNumber" => value == null ? query.Where(s => s.AgreementNumber == null) : query.Where(s => s.AgreementNumber == value),
                "TerritoryCode" => value == null ? query.Where(s => s.TerritoryCode == null) : query.Where(s => s.TerritoryCode == value),
                "ProductId" => value == null ? query.Where(s => s.ProductId == null) : query.Where(s => s.ProductId == int.Parse(value)),
                _ => query
            };
        }

        return query;
    }

    private static List<SqlParameter> CloneParameters(List<SqlParameter> source)
    {
        return source.Select(p => new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value)).ToList();
    }
}
