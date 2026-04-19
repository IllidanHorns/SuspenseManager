namespace Common.DTOs;

/// <summary>
/// Результат разбора Excel: проверка заголовков и строки DTO.
/// </summary>
public class ExcelParseResult
{
    /// <summary>
    /// Все обязательные столбцы (по шаблону импорта) найдены в первой строке.
    /// </summary>
    public bool HeadersValid => MissingRequiredHeaders.Count == 0;

    /// <summary>
    /// Описания отсутствующих столбцов (какие подписи допустимы — для сообщения пользователю).
    /// </summary>
    public List<string> MissingRequiredHeaders { get; init; } = [];

    public List<SuspenseLineDto> Lines { get; init; } = [];
}
