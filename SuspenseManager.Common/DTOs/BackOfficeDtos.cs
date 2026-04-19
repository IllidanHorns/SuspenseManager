namespace Common.DTOs;

/// <summary>
/// Запрос на получение списка заданий в бэк-офисе
/// </summary>
public class BackOfficeTasksRequest : PagedRequest
{
    /// <summary>
    /// Фильтр по типу: null = все, 120 = нет продукта, 320 = нет прав
    /// </summary>
    public int? TaskType { get; set; }
}

/// <summary>
/// DTO задания в бэк-офисе — для списка и детальной карточки
/// </summary>
public class BackOfficeTaskDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }

    /// <summary>
    /// Текущий статус группы (120 или 320)
    /// </summary>
    public int GroupStatus { get; set; }

    public int SuspenseCount { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Статус задания: 0=Open, 1=ReturnedToOperator, 2=GroupDeleted, 3=Completed
    /// </summary>
    public int TaskStatus { get; set; }

    public DateTime CreateTime { get; set; }

    // Данные об операторе, отправившем задание
    public int CreatedByAccountId { get; set; }
    public string CreatedByLogin { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }

    // Краткие данные группы (из метаданных или первого суспенса)
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Isrc { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// DTO для привязки продукта к группе через BO-задание
/// </summary>
public class BoLinkProductDto
{
    public int ProductId { get; set; }
}

/// <summary>
/// DTO для копирования прав через BO-задание
/// </summary>
public class BoCopyRightsDto
{
    public int RightsId { get; set; }
}
