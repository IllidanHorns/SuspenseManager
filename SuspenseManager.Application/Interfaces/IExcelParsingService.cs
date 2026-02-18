using System.Collections.Generic;
using System.IO;
using Common.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Сервис парсинга Excel-файлов с отчётами стриминговых платформ
/// </summary>
public interface IExcelParsingService
{
    /// <summary>
    /// Парсинг Excel-файла в список DTO суспенсов
    /// </summary>
    List<SuspenseLineDto> ParseExcel(Stream fileStream);
}
