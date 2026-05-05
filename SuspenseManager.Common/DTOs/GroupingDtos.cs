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

    /// <summary>Минимальное кол-во строк в группе (HAVING)</summary>
    public int? CountMin { get; set; }

    /// <summary>Максимальное кол-во строк в группе (HAVING)</summary>
    public int? CountMax { get; set; }

    /// <summary>Минимальная выручка группы в рублях (HAVING)</summary>
    public decimal? RevenueMin { get; set; }

    /// <summary>Максимальная выручка группы в рублях (HAVING)</summary>
    public decimal? RevenueMax { get; set; }
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

    /// <summary>
    /// Суммарная выручка по группе в рублях: SUM(Qty * PPD * ExchangeRate)
    /// </summary>
    public decimal RevenueRub { get; set; }
}

/// <summary>
/// Запрос на предпросмотр строк суспенса внутри потенциальной группы
/// </summary>
public class GroupLinesPreviewRequest
{
    public int BusinessStatus { get; set; }
    public List<string> GroupByColumns { get; set; } = [];
    public Dictionary<string, string?> KeyValues { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Строка суспенса для предпросмотра группы
/// </summary>
public class SuspenseLinePreviewDto
{
    public int Id { get; set; }
    public string? Isrc { get; set; }
    public string? Barcode { get; set; }
    public string? CatalogNumber { get; set; }
    public string? Artist { get; set; }
    public string? TrackTitle { get; set; }
    public string? Genre { get; set; }
    public string? Operator { get; set; }
    public string? SenderCompany { get; set; }
    public string? RecipientCompany { get; set; }
    public string? TerritoryCode { get; set; }
    public string? AgreementType { get; set; }
    public string? AgreementNumber { get; set; }
    public int Qty { get; set; }
    public double? Ppd { get; set; }
    public string? ExchangeCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
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
