namespace Models;

/// <summary>
/// Лог изменений статуса строки суспенса.
/// Каждая запись фиксирует один переход статуса: кто, когда, из какого статуса в какой.
/// </summary>
public class SuspenseLineLog
{
    /// <summary>PK</summary>
    public int Id { get; set; }

    /// <summary>FK — строка суспенса</summary>
    public int SuspenseLineId { get; set; }

    /// <summary>Навигационное свойство</summary>
    public SuspenseLine SuspenseLine { get; set; } = null!;

    /// <summary>
    /// FK группы на момент события (сохраняется для истории,
    /// т.к. после разгруппировки SuspenseLine.GroupId обнуляется)
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>Статус до изменения (null при первичной загрузке)</summary>
    public int? StatusFrom { get; set; }

    /// <summary>Статус после изменения</summary>
    public int StatusTo { get; set; }

    /// <summary>FK аккаунта, который выполнил действие (денормализован)</summary>
    public int AccountId { get; set; }

    /// <summary>Логин пользователя на момент события (денормализован)</summary>
    public string AccountLogin { get; set; } = null!;

    /// <summary>Имя и фамилия пользователя на момент события (денормализованы)</summary>
    public string? AccountName { get; set; }

    /// <summary>Время выполнения операции</summary>
    public DateTime OperationTime { get; set; }
}
