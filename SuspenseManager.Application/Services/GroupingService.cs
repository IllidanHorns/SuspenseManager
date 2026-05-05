using Application.Helpers;
using Application.Interfaces;
using ClosedXML.Excel;
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
    private readonly IAuditService _audit;

    public GroupingService(SuspenseManagerDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
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
            request.PageSize,
            request.CountMin,
            request.CountMax,
            request.RevenueMin,
            request.RevenueMax);

        // Нужны отдельные параметры для count, т.к. SqlParameter нельзя использовать повторно
        var countParams = CloneParameters(parameters
            .Where(p => p.ParameterName != "@pOffset" && p.ParameterName != "@pPageSize")
            .ToList());

        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        // COUNT-запрос (количество групп)
        int totalCount;
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = countSql;
            countCmd.Parameters.AddRange(countParams.ToArray());
            var countResult = await countCmd.ExecuteScalarAsync(ct);
            totalCount = countResult is null or DBNull ? 0 : Convert.ToInt32(countResult);
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
                var revenueOrdinal = request.GroupByColumns.Count + 1;
                item.RevenueRub = reader.IsDBNull(revenueOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(revenueOrdinal));
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

    public async Task<PagedResponse<SuspenseLinePreviewDto>> PreviewLinesAsync(
        GroupLinesPreviewRequest request, CancellationToken ct = default)
    {
        GroupingSqlBuilder.ValidateRequest(request.BusinessStatus, request.GroupByColumns);

        var (whereClause, whereParams) = GroupingSqlBuilder.BuildCommitWhereClause(
            request.BusinessStatus, request.GroupByColumns, request.KeyValues);

        var fromClause = request.BusinessStatus == 0
            ? "FROM [SuspenseLines] s"
            : "FROM [SuspenseLines] s INNER JOIN [CatalogProducts] cp ON s.[ProductId] = cp.[Id]";

        var baseWhere =
            $"s.[BusinessStatus] = {request.BusinessStatus} AND s.[ArchiveLevel] = 0 AND s.[GroupId] IS NULL" +
            (whereClause.Length > 0 ? $" AND {whereClause}" : "");

        var offset = (request.PageNumber - 1) * request.PageSize;

        var sql = $"""
            SELECT s.[Id], s.[Isrc], s.[Barcode], s.[CatalogNumber], s.[Artist], s.[TrackTitle],
                   s.[Genre], s.[Operator], s.[SenderCompany], s.[RecipientCompany],
                   s.[TerritoryCode], s.[AgreementType], s.[AgreementNumber],
                   s.[Qty], s.[Ppd], s.[ExchangeCurrency], s.[ExchangeRate]
            {fromClause}
            WHERE {baseWhere}
            ORDER BY s.[Id]
            OFFSET {offset} ROWS FETCH NEXT {request.PageSize} ROWS ONLY
            """;

        var countSql = $"SELECT COUNT(*) {fromClause} WHERE {baseWhere}";

        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        int totalCount;
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = countSql;
            cmd.Parameters.AddRange(CloneParameters(whereParams).ToArray());
            totalCount = (int)await cmd.ExecuteScalarAsync(ct)!;
        }

        var items = new List<SuspenseLinePreviewDto>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(CloneParameters(whereParams).ToArray());
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new SuspenseLinePreviewDto
                {
                    Id              = reader.GetInt32(0),
                    Isrc            = reader.IsDBNull(1)  ? null : reader.GetString(1),
                    Barcode         = reader.IsDBNull(2)  ? null : reader.GetString(2),
                    CatalogNumber   = reader.IsDBNull(3)  ? null : reader.GetString(3),
                    Artist          = reader.IsDBNull(4)  ? null : reader.GetString(4),
                    TrackTitle      = reader.IsDBNull(5)  ? null : reader.GetString(5),
                    Genre           = reader.IsDBNull(6)  ? null : reader.GetString(6),
                    Operator        = reader.IsDBNull(7)  ? null : reader.GetString(7),
                    SenderCompany   = reader.IsDBNull(8)  ? null : reader.GetString(8),
                    RecipientCompany= reader.IsDBNull(9)  ? null : reader.GetString(9),
                    TerritoryCode   = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AgreementType   = reader.IsDBNull(11) ? null : reader.GetString(11),
                    AgreementNumber = reader.IsDBNull(12) ? null : reader.GetString(12),
                    Qty             = reader.GetInt32(13),
                    Ppd             = reader.IsDBNull(14) ? null : reader.GetDouble(14),
                    ExchangeCurrency= reader.IsDBNull(15) ? null : reader.GetString(15),
                    ExchangeRate    = reader.IsDBNull(16) ? 0m   : Convert.ToDecimal(reader.GetValue(16)),
                });
            }
        }

        return new PagedResponse<SuspenseLinePreviewDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<byte[]> ExportPreviewLinesAsync(
        GroupLinesPreviewRequest request, CancellationToken ct = default)
    {
        GroupingSqlBuilder.ValidateRequest(request.BusinessStatus, request.GroupByColumns);

        var (whereClause, whereParams) = GroupingSqlBuilder.BuildCommitWhereClause(
            request.BusinessStatus, request.GroupByColumns, request.KeyValues);

        var fromClause = request.BusinessStatus == 0
            ? "FROM [SuspenseLines] s"
            : "FROM [SuspenseLines] s INNER JOIN [CatalogProducts] cp ON s.[ProductId] = cp.[Id]";

        var baseWhere =
            $"s.[BusinessStatus] = {request.BusinessStatus} AND s.[ArchiveLevel] = 0 AND s.[GroupId] IS NULL" +
            (whereClause.Length > 0 ? $" AND {whereClause}" : "");

        var sql = $"""
            SELECT s.[Id], s.[Isrc], s.[Barcode], s.[CatalogNumber], s.[Artist], s.[TrackTitle],
                   s.[Genre], s.[Operator], s.[SenderCompany], s.[RecipientCompany],
                   s.[TerritoryCode], s.[AgreementType], s.[AgreementNumber],
                   s.[Qty], s.[Ppd], s.[ExchangeCurrency], s.[ExchangeRate]
            {fromClause}
            WHERE {baseWhere}
            ORDER BY s.[Id]
            """;

        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        var items = new List<SuspenseLinePreviewDto>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(CloneParameters(whereParams).ToArray());
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new SuspenseLinePreviewDto
                {
                    Id               = reader.GetInt32(0),
                    Isrc             = reader.IsDBNull(1)  ? null : reader.GetString(1),
                    Barcode          = reader.IsDBNull(2)  ? null : reader.GetString(2),
                    CatalogNumber    = reader.IsDBNull(3)  ? null : reader.GetString(3),
                    Artist           = reader.IsDBNull(4)  ? null : reader.GetString(4),
                    TrackTitle       = reader.IsDBNull(5)  ? null : reader.GetString(5),
                    Genre            = reader.IsDBNull(6)  ? null : reader.GetString(6),
                    Operator         = reader.IsDBNull(7)  ? null : reader.GetString(7),
                    SenderCompany    = reader.IsDBNull(8)  ? null : reader.GetString(8),
                    RecipientCompany = reader.IsDBNull(9)  ? null : reader.GetString(9),
                    TerritoryCode    = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AgreementType    = reader.IsDBNull(11) ? null : reader.GetString(11),
                    AgreementNumber  = reader.IsDBNull(12) ? null : reader.GetString(12),
                    Qty              = reader.GetInt32(13),
                    Ppd              = reader.IsDBNull(14) ? null  : reader.GetDouble(14),
                    ExchangeCurrency = reader.IsDBNull(15) ? null  : reader.GetString(15),
                    ExchangeRate     = reader.IsDBNull(16) ? 0m    : Convert.ToDecimal(reader.GetValue(16)),
                });
            }
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Суспенсы");
        var headers = new[]
        {
            "ID", "ISRC", "Баркод", "Каталожный номер", "Артист", "Название трека",
            "Жанр", "Оператор", "Отправитель", "Получатель",
            "Территория", "Тип договора", "Номер договора", "Кол-во", "PPD", "Валюта", "Курс обмена"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var row = 0; row < items.Count; row++)
        {
            var s = items[row];
            var r = row + 2;
            ws.Cell(r, 1).Value = s.Id;
            ws.Cell(r, 2).Value = s.Isrc;
            ws.Cell(r, 3).Value = s.Barcode;
            ws.Cell(r, 4).Value = s.CatalogNumber;
            ws.Cell(r, 5).Value = s.Artist;
            ws.Cell(r, 6).Value = s.TrackTitle;
            ws.Cell(r, 7).Value = s.Genre;
            ws.Cell(r, 8).Value = s.Operator;
            ws.Cell(r, 9).Value = s.SenderCompany;
            ws.Cell(r, 10).Value = s.RecipientCompany;
            ws.Cell(r, 11).Value = s.TerritoryCode;
            ws.Cell(r, 12).Value = s.AgreementType;
            ws.Cell(r, 13).Value = s.AgreementNumber;
            ws.Cell(r, 14).Value = s.Qty;
            ws.Cell(r, 15).Value = s.Ppd;
            ws.Cell(r, 16).Value = s.ExchangeCurrency;
            ws.Cell(r, 17).Value = s.ExchangeRate;
        }
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
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
        {
            throw new BusinessException(
                "Не найдено суспенсов, соответствующих критериям группировки",
                "NO_MATCHING_SUSPENSES");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Определяем CatalogProductId для статуса 1
            int? catalogProductId = null;
            if (request.BusinessStatus == 1)
            {
                catalogProductId = suspenses[0].ProductId;

                // Проверяем что продукт реально существует и не архивирован
                var productExists = catalogProductId.HasValue &&
                    await _db.CatalogProducts
                        .AnyAsync(p => p.Id == catalogProductId.Value && p.ArchiveLevel == 0, ct);

                if (!productExists)
                {
                    throw new BusinessException(
                        "Продукт, связанный с суспенсами этой группы, не найден или архивирован",
                        "PRODUCT_NOT_FOUND", 404);
                }
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

            // Создаём начальные метаданные из значений группировки
            var meta = BuildInitialMetadata(group.Id, request);
            _db.GroupMetadata.Add(meta);
            await _db.SaveChangesAsync(ct);

            group.MetaDataId = meta.Id;
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

            // Логируем создание группы и начальный статус всех строк
            await _audit.LogGroupAsync(group.Id, null, newStatus, ct);
            await _audit.LogGroupLinesAsync(group.Id, null, newStatus, ct);

            return group;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static GroupMetadata BuildInitialMetadata(int groupId, GroupingCommitRequest request)
    {
        var kv = request.KeyValues;

        // TrackTitle → Title для статуса 0; ProductName → Title для статуса 1
        kv.TryGetValue("TrackTitle", out var trackTitle);
        kv.TryGetValue("ProductName", out var productName);

        return new GroupMetadata
        {
            SuspenseGroupId = groupId,
            Isrc           = kv.GetValueOrDefault("Isrc"),
            Barcode        = kv.GetValueOrDefault("Barcode"),
            CatalogNumber  = kv.GetValueOrDefault("CatalogNumber"),
            Artist         = kv.GetValueOrDefault("Artist"),
            Title          = trackTitle ?? productName,
            Genre          = kv.GetValueOrDefault("Genre"),
            CreateTime     = DateTime.UtcNow,
            ChangeTime     = DateTime.UtcNow,
        };
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
                        && s.GroupId == null
                        && s.ProductId == null);

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
            {
                continue;
            }

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
            {
                continue;
            }

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
                "ProductId" => value == null ? query.Where(s => s.ProductId == null) : int.TryParse(value, out var pid) ? query.Where(s => s.ProductId == pid) : query,
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
