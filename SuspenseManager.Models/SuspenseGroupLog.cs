namespace Models;

/// <summary>
/// Лог изменений статуса группы суспенсов.
/// Каждая запись фиксирует один переход статуса: кто, когда, из какого статуса в какой.
/// </summary>
public class SuspenseGroupLog
{
    /// <summary>PK</summary>
    public int Id { get; set; }

    /// <summary>FK — группа суспенсов</summary>
    public int SuspenseGroupId { get; set; }

    /// <summary>Навигационное свойство</summary>
    public SuspenseGroup SuspenseGroup { get; set; } = null!;

    /// <summary>Статус до изменения (null при создании группы)</summary>
    public int? StatusFrom { get; set; }

    /// <summary>Статус после изменения</summary>
    public int StatusTo { get; set; }

    /// <summary>FK аккаунта, который выполнил действие (денормализован на случай удаления аккаунта)</summary>
    public int AccountId { get; set; }

    /// <summary>Логин пользователя на момент события (денормализован)</summary>
    public string AccountLogin { get; set; } = null!;

    /// <summary>Имя и фамилия пользователя на момент события (денормализованы)</summary>
    public string? AccountName { get; set; }

    /// <summary>Время выполнения операции</summary>
    public DateTime OperationTime { get; set; }
}
