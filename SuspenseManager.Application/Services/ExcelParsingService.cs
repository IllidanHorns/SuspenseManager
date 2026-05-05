using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.Interfaces;
using ClosedXML.Excel;
using Common.DTOs;

namespace Application.Services;

/// <summary>
/// Парсинг Excel-файлов с отчётами стриминговых платформ.
/// Ожидает первую строку как заголовки, данные начинаются со второй строки.
/// </summary>
public class ExcelParsingService : IExcelParsingService
{
    private static readonly Dictionary<string, string> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
{
    { "ISRC", nameof(SuspenseLineDto.Isrc) },
    { "Баркод", nameof(SuspenseLineDto.Barcode) },
    { "Barcode", nameof(SuspenseLineDto.Barcode) },
    { "Каталожный номер", nameof(SuspenseLineDto.CatalogNumber) },
    { "CatalogNumber", nameof(SuspenseLineDto.CatalogNumber) },
    { "Формат продукта", nameof(SuspenseLineDto.ProductFormatCode) },
    { "ProductFormatCode", nameof(SuspenseLineDto.ProductFormatCode) },
    { "TTkey", nameof(SuspenseLineDto.ProductFormatCode) },
    { "Компания отправитель", nameof(SuspenseLineDto.SenderCompany) },
    { "SenderCompany", nameof(SuspenseLineDto.SenderCompany) },
    { "Компания получатель", nameof(SuspenseLineDto.RecipientCompany) },
    { "RecipientCompany", nameof(SuspenseLineDto.RecipientCompany) },
    { "Оператор", nameof(SuspenseLineDto.Operator) },
    { "Operator", nameof(SuspenseLineDto.Operator) },
    { "Артист", nameof(SuspenseLineDto.Artist) },
    { "Artist", nameof(SuspenseLineDto.Artist) },
    { "Название", nameof(SuspenseLineDto.TrackTitle) },
    { "TrackTitle", nameof(SuspenseLineDto.TrackTitle) },
    { "Тип договора", nameof(SuspenseLineDto.AgreementType) },
    { "AgreementType", nameof(SuspenseLineDto.AgreementType) },
    { "Номер договора", nameof(SuspenseLineDto.AgreementNumber) },
    { "AgreementNumber", nameof(SuspenseLineDto.AgreementNumber) },
    { "Код территории", nameof(SuspenseLineDto.TerritoryCode) },
    { "TerritoryCode", nameof(SuspenseLineDto.TerritoryCode) },
    { "Количество", nameof(SuspenseLineDto.Qty) },
    { "Qty", nameof(SuspenseLineDto.Qty) },
    { "Цена за стрим", nameof(SuspenseLineDto.Ppd) },
    { "Ppd", nameof(SuspenseLineDto.Ppd) },
    { "Валюта", nameof(SuspenseLineDto.ExchangeCurrency) },
    { "ExchangeCurrency", nameof(SuspenseLineDto.ExchangeCurrency) },
    { "Курс обмена", nameof(SuspenseLineDto.ExchangeRate) },
    { "ExchangeRate", nameof(SuspenseLineDto.ExchangeRate) },
    { "Жанр", nameof(SuspenseLineDto.Genre) },
    { "Genre", nameof(SuspenseLineDto.Genre) },
};

    /// <summary>
    /// Свойство DTO → допустимые подписи столбца в первой строке.
    /// </summary>
    private static readonly Lazy<Dictionary<string, string[]>> PropertyToAliases = new(() =>
        ColumnAliases
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).Distinct().ToArray()));

    public ExcelParseResult ParseExcel(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        if (!workbook.Worksheets.Any())
            throw new ArgumentException("Excel-файл не содержит листов.", nameof(fileStream));

        var worksheet = workbook.Worksheets.First();

        var missing = GetMissingRequiredHeaders(worksheet);
        if (missing.Count > 0)
        {
            return new ExcelParseResult { MissingRequiredHeaders = missing, Lines = [] };
        }

        var columnMap = BuildColumnMap(worksheet);

        var result = new List<SuspenseLineDto>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var row = 2; row <= lastRow; row++)
        {
            var dto = ParseRow(worksheet, row, columnMap);
            if (dto != null)
            {
                result.Add(dto);
            }
        }

        return new ExcelParseResult { MissingRequiredHeaders = [], Lines = result };
    }

    /// <summary>
    /// Для каждого поля импорта должна быть хотя бы одна колонка с подписью из списка синонимов.
    /// </summary>
    private static List<string> GetMissingRequiredHeaders(IXLWorksheet worksheet)
    {
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var headerSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= lastCol; col++)
        {
            var header = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            headerSet.Add(header);
        }

        var missing = new List<string>();
        foreach (var kv in PropertyToAliases.Value.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var aliases = kv.Value;
            if (!aliases.Any(headerSet.Contains))
            {
                missing.Add($"{string.Join(" · ", aliases)}");
            }
        }

        return missing;
    }

    /// <summary>
    /// Строит маппинг: название свойства DTO → номер столбца в Excel
    /// </summary>
    private static Dictionary<string, int> BuildColumnMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>();
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var col = 1; col <= lastCol; col++)
        {
            var header = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            if (ColumnAliases.TryGetValue(header, out var propertyName))
            {
                map.TryAdd(propertyName, col);
            }
        }

        return map;
    }

    /// <summary>
    /// Парсинг одной строки Excel в DTO
    /// </summary>
    private static SuspenseLineDto? ParseRow(IXLWorksheet ws, int row, Dictionary<string, int> columnMap)
    {
        if (ws.Row(row).IsEmpty())
        {
            return null;
        }

        var dto = new SuspenseLineDto
        {
            Isrc = GetString(ws, row, columnMap, nameof(SuspenseLineDto.Isrc)),
            Barcode = GetString(ws, row, columnMap, nameof(SuspenseLineDto.Barcode)),
            CatalogNumber = GetString(ws, row, columnMap, nameof(SuspenseLineDto.CatalogNumber)),
            ProductFormatCode = GetString(ws, row, columnMap, nameof(SuspenseLineDto.ProductFormatCode)),
            SenderCompany = GetString(ws, row, columnMap, nameof(SuspenseLineDto.SenderCompany)),
            RecipientCompany = GetString(ws, row, columnMap, nameof(SuspenseLineDto.RecipientCompany)),
            Operator = GetString(ws, row, columnMap, nameof(SuspenseLineDto.Operator)),
            Artist = GetString(ws, row, columnMap, nameof(SuspenseLineDto.Artist)),
            TrackTitle = GetString(ws, row, columnMap, nameof(SuspenseLineDto.TrackTitle)),
            AgreementType = GetString(ws, row, columnMap, nameof(SuspenseLineDto.AgreementType)),
            AgreementNumber = GetString(ws, row, columnMap, nameof(SuspenseLineDto.AgreementNumber)),
            TerritoryCode = GetString(ws, row, columnMap, nameof(SuspenseLineDto.TerritoryCode)),
            Genre = GetString(ws, row, columnMap, nameof(SuspenseLineDto.Genre)),
            Qty = GetInt(ws, row, columnMap, nameof(SuspenseLineDto.Qty)),
            Ppd = GetDouble(ws, row, columnMap, nameof(SuspenseLineDto.Ppd)),
            ExchangeCurrency = GetDecimal(ws, row, columnMap, nameof(SuspenseLineDto.ExchangeCurrency)),
            ExchangeRate = GetDecimal(ws, row, columnMap, nameof(SuspenseLineDto.ExchangeRate)),
        };

        return dto;
    }

    private static string? GetString(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
        {
            return null;
        }

        var value = ws.Cell(row, col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int GetInt(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
        {
            return 0;
        }

        return ws.Cell(row, col).TryGetValue(out int value) ? value : 0;
    }

    private static double? GetDouble(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
        {
            return null;
        }

        return ws.Cell(row, col).TryGetValue(out double value) ? value : null;
    }

    private static decimal GetDecimal(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
        {
            return 0;
        }

        var cell = ws.Cell(row, col);
        if (cell.TryGetValue(out double value))
        {
            try { return (decimal)value; }
            catch (OverflowException) { return 0; }
        }

        var text = cell.GetString()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("ru-RU"), out var ru) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out ru))
        {
            return ru;
        }

        return 0;
    }
}
