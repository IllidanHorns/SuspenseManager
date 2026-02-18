using System;
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
    // ISRC — оставляем один вариант (регистр не важен благодаря StringComparer.OrdinalIgnoreCase)
    { "ISRC", nameof(SuspenseLineDto.Isrc) },
    
    // Barcode — разные строки (рус/англ), не конфликтуют
    { "Баркод", nameof(SuspenseLineDto.Barcode) },
    { "Barcode", nameof(SuspenseLineDto.Barcode) },
    
    // CatalogNumber
    { "Каталожный номер", nameof(SuspenseLineDto.CatalogNumber) },
    { "CatalogNumber", nameof(SuspenseLineDto.CatalogNumber) },
    
    // ProductFormatCode
    { "Формат продукта", nameof(SuspenseLineDto.ProductFormatCode) },
    { "ProductFormatCode", nameof(SuspenseLineDto.ProductFormatCode) },
    { "TTkey", nameof(SuspenseLineDto.ProductFormatCode) },
    
    // SenderCompany
    { "Компания отправитель", nameof(SuspenseLineDto.SenderCompany) },
    { "SenderCompany", nameof(SuspenseLineDto.SenderCompany) },
    
    // RecipientCompany
    { "Компания получатель", nameof(SuspenseLineDto.RecipientCompany) },
    { "RecipientCompany", nameof(SuspenseLineDto.RecipientCompany) },
    
    // Operator
    { "Оператор", nameof(SuspenseLineDto.Operator) },
    { "Operator", nameof(SuspenseLineDto.Operator) },
    
    // Artist
    { "Артист", nameof(SuspenseLineDto.Artist) },
    { "Artist", nameof(SuspenseLineDto.Artist) },
    
    // TrackTitle
    { "Название", nameof(SuspenseLineDto.TrackTitle) },
    { "TrackTitle", nameof(SuspenseLineDto.TrackTitle) },
    
    // AgreementType
    { "Тип договора", nameof(SuspenseLineDto.AgreementType) },
    { "AgreementType", nameof(SuspenseLineDto.AgreementType) },
    
    // AgreementNumber
    { "Номер договора", nameof(SuspenseLineDto.AgreementNumber) },
    { "AgreementNumber", nameof(SuspenseLineDto.AgreementNumber) },
    
    // TerritoryCode
    { "Код территории", nameof(SuspenseLineDto.TerritoryCode) },
    { "TerritoryCode", nameof(SuspenseLineDto.TerritoryCode) },
    
    // Qty
    { "Количество", nameof(SuspenseLineDto.Qty) },
    { "Qty", nameof(SuspenseLineDto.Qty) },
    
    // Ppd
    { "Цена за стрим", nameof(SuspenseLineDto.Ppd) },
    { "Ppd", nameof(SuspenseLineDto.Ppd) },
    
    // ExchangeCurrency
    { "Валюта", nameof(SuspenseLineDto.ExchangeCurrency) },
    { "ExchangeCurrency", nameof(SuspenseLineDto.ExchangeCurrency) },
    
    // ExchangeRate
    { "Курс обмена", nameof(SuspenseLineDto.ExchangeRate) },
    { "ExchangeRate", nameof(SuspenseLineDto.ExchangeRate) },
    
    // Genre
    { "Жанр", nameof(SuspenseLineDto.Genre) },
    { "Genre", nameof(SuspenseLineDto.Genre) },
};

    public List<SuspenseLineDto> ParseExcel(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();

        // Считываем заголовки из первой строки
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

        return result;
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
                continue;

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
        // Пропускаем полностью пустые строки
        if (ws.Row(row).IsEmpty())
            return null;

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
            return null;

        var value = ws.Cell(row, col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int GetInt(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
            return 0;

        return ws.Cell(row, col).TryGetValue(out int value) ? value : 0;
    }

    private static double? GetDouble(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
            return null;

        return ws.Cell(row, col).TryGetValue(out double value) ? value : null;
    }

    private static decimal GetDecimal(IXLWorksheet ws, int row, Dictionary<string, int> map, string property)
    {
        if (!map.TryGetValue(property, out var col))
            return 0;

        // ClosedXML не имеет TryGetValue<decimal>, поэтому через double
        if (ws.Cell(row, col).TryGetValue(out double value))
            return (decimal)value;

        return 0;
    }
}
