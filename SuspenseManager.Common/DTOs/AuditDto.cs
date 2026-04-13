namespace Common.DTOs;

// ── Запрос для списка групп аудита ────────────────────────────────────���─────

public class AuditGroupsRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 20;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { <= 0 => 20, > 100 => 100, _ => value };
    }

    /// <summary>Фильтр по статусу группы</summary>
    public int? Status { get; set; }

    /// <summary>Фильтр по создателю группы (accountId)</summary>
    public int? CreatedByAccountId { get; set; }

    /// <summary>Показать только группы текущего пользователя</summary>
    public bool OnlyMine { get; set; }

    /// <summary>Дата создания от (UTC)</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>Дата создания до (UTC)</summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>Дата последнего изменения от (UTC)</summary>
    public DateTime? LastChangedFrom { get; set; }

    /// <summary>Дата последнего изменения до (UTC)</summary>
    public DateTime? LastChangedTo { get; set; }
}

// ── Запрос для списка строк аудита ──────────────────────────────────────────

public class AuditLinesRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 20;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { <= 0 => 20, > 100 => 100, _ => value };
    }

    /// <summary>Фильтр по статусу строки</summary>
    public int? Status { get; set; }

    /// <summary>Фильтр по группе</summary>
    public int? GroupId { get; set; }

    /// <summary>Показать только строки из групп текущего пользователя</summary>
    public bool OnlyMine { get; set; }

    /// <summary>Дата последнего изменения от (UTC)</summary>
    public DateTime? LastChangedFrom { get; set; }

    /// <summary>Дата последнего изменения до (UTC)</summary>
    public DateTime? LastChangedTo { get; set; }
}

// ── DTO группы в списке аудита ───────────────────────────────────────────────

public class AuditGroupDto
{
    public int Id { get; set; }
    public int BusinessStatus { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreateTime { get; set; }

    // Создатель
    public int CreatedByAccountId { get; set; }
    public string CreatedByLogin { get; set; } = null!;
    public string? CreatedByName { get; set; }

    // Последний лог
    public DateTime? LastChangeTime { get; set; }
    public int? LastStatusFrom { get; set; }
    public string? LastStatusFromName { get; set; }
    public int? LastStatusTo { get; set; }
    public string? LastStatusToName { get; set; }
    public string? LastChangedByLogin { get; set; }
    public string? LastChangedByName { get; set; }

    // Продукт (если привязан)
    public int? CatalogProductId { get; set; }
    public string? ProductName { get; set; }
}

// ── DTO строки суспенса в списке аудита ─────────────────────────────────────

public class AuditLineDto
{
    public int Id { get; set; }
    public int BusinessStatus { get; set; }
    public string? StatusName { get; set; }
    public int? GroupId { get; set; }

    // Идентификаторы записи
    public string? Isrc { get; set; }
    public string? Barcode { get; set; }
    public string? Artist { get; set; }
    public string? TrackTitle { get; set; }
    public string? Operator { get; set; }

    public DateTime CreateTime { get; set; }

    // Создатель группы (для фильтра "только мои")
    public int? GroupCreatedByAccountId { get; set; }
    public string? GroupCreatedByLogin { get; set; }

    // Последний лог
    public DateTime? LastChangeTime { get; set; }
    public int? LastStatusFrom { get; set; }
    public string? LastStatusFromName { get; set; }
    public int? LastStatusTo { get; set; }
    public string? LastStatusToName { get; set; }
    public string? LastChangedByLogin { get; set; }
    public string? LastChangedByName { get; set; }
}

// ── DTO одной записи лога ───────────────────────────────────────────────────

public class AuditLogEntryDto
{
    public int Id { get; set; }
    public int? StatusFrom { get; set; }
    public string? StatusFromName { get; set; }
    public int StatusTo { get; set; }
    public string? StatusToName { get; set; }
    public int AccountId { get; set; }
    public string AccountLogin { get; set; } = null!;
    public string? AccountName { get; set; }
    public DateTime OperationTime { get; set; }
}
