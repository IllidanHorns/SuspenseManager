using System.Collections.Generic;

namespace Common.DTOs;

/// <summary>
/// Результат валидации пакета строк
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Общее количество обработанных строк
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Количество строк со статусом "валидация пройдена" (88)
    /// </summary>
    public int ValidatedCount { get; set; }

    /// <summary>
    /// Количество строк со статусом "нет продукта" (0)
    /// </summary>
    public int NoProductCount { get; set; }

    /// <summary>
    /// Количество строк со статусом "нет прав" (1)
    /// </summary>
    public int NoRightsCount { get; set; }

    /// <summary>
    /// Детали по каждой строке
    /// </summary>
    public List<ValidationLineResultDto> Lines { get; set; } = new();
}

/// <summary>
/// Результат валидации одной строки
/// </summary>
public class ValidationLineResultDto
{
    /// <summary>
    /// ID созданного суспенса
    /// </summary>
    public int SuspenseLineId { get; set; }

    /// <summary>
    /// Присвоенный статус
    /// </summary>
    public int BusinessStatus { get; set; }

    /// <summary>
    /// Причина попадания в суспенс
    /// </summary>
    public string CauseSuspense { get; set; } = string.Empty;

    /// <summary>
    /// ID найденного продукта (если найден)
    /// </summary>
    public int? ProductId { get; set; }
}
