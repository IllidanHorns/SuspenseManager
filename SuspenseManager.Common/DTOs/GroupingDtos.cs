namespace Common.DTOs;

/// <summary>
/// Запрос на предпросмотр динамической группировки суспенсов
/// </summary>
public class GroupingPreviewRequest : PagedRequest
{
    /// <summary>
    /// Бизнес-статус суспенсов для группировки (0 — нет продукта, 1 — нет прав)
    /// </summary>
    public int BusinessStatus { get; set; }

    /// <summary>
    /// Набор столбцов для группировки.
    /// Для статуса 1 обязательно должен содержать "ProductId".
    /// </summary>
    public List<string> GroupByColumns { get; set; } = [];
}

/// <summary>
/// Одна строка результата динамической группировки
/// </summary>
public class GroupingPreviewItem
{
    /// <summary>
    /// Значения столбцов группировки (ключ — имя столбца, значение — значение)
    /// </summary>
    public Dictionary<string, string?> Key { get; set; } = new();

    /// <summary>
    /// Количество суспенсов в группе
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Запрос на фиксацию (сохранение) динамической группы
/// </summary>
public class GroupingCommitRequest
{
    /// <summary>
    /// Бизнес-статус (0 — нет продукта, 1 — нет прав)
    /// </summary>
    public int BusinessStatus { get; set; }

    /// <summary>
    /// Столбцы группировки (для формирования WHERE-условия поиска строк)
    /// </summary>
    public List<string> GroupByColumns { get; set; } = [];

    /// <summary>
    /// Значения столбцов группировки — определяют конкретную группу для фиксации
    /// </summary>
    public Dictionary<string, string?> KeyValues { get; set; } = new();

    /// <summary>
    /// ID аккаунта пользователя, создающего группу
    /// </summary>
    public int AccountId { get; set; }
}
