namespace Models.Enums;

/// <summary>
/// Статусы задания в бэк-офисе
/// </summary>
public enum BackOfficeTaskStatus
{
    /// <summary>
    /// Задание открыто, ожидает обработки
    /// </summary>
    Open = 0,

    /// <summary>
    /// Группа возвращена оператору (120→15 или 320→16)
    /// </summary>
    ReturnedToOperator = 1,

    /// <summary>
    /// Группа и строки удалены через мягкое удаление
    /// </summary>
    GroupDeleted = 2,

    /// <summary>
    /// Группа успешно валидирована (достигла статуса 88)
    /// </summary>
    Completed = 3,
}
