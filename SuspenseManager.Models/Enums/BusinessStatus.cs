namespace Models.Enums;

/// <summary>
/// Бизнес-статусы суспенсов и групп
/// </summary>
public enum BusinessStatus
{
    /// <summary>
    /// Суспенс не в группе, нет продукта
    /// </summary>
    NoProduct = 0,

    /// <summary>
    /// Суспенс не в группе, нет прав
    /// </summary>
    NoRights = 1,

    /// <summary>
    /// Суспенс в группе, нет продукта3
    /// </summary>
    InGroupNoProduct = 15,

    /// <summary>
    /// Суспенс в группе, нет прав
    /// </summary>
    InGroupNoRights = 16,

    /// <summary>
    /// Группа отложена, нет продукта
    /// </summary>
    PostponedNoProduct = 30,

    /// <summary>
    /// Группа отложена, нет прав
    /// </summary>
    PostponedNoRights = 32,

    /// <summary>
    /// Валидация пройдена успешно
    /// </summary>
    Validated = 88,

    /// <summary>
    /// Передан в бэк-офис, нет продукта
    /// </summary>
    BackOfficeNoProduct = 120,

    /// <summary>
    /// Передан в бэк-офис, нет прав
    /// </summary>
    BackOfficeNoRights = 320,
}
