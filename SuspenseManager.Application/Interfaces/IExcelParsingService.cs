using System.IO;
using Common.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Сервис парсинга Excel-файлов с отчётами стриминговых платформ
/// </summary>
public interface IExcelParsingService
{
    /// <summary>
    /// Парсинг Excel-файла: проверка обязательных заголовков и разбор строк в DTO.
    /// </summary>
    ExcelParseResult ParseExcel(Stream fileStream);
}
