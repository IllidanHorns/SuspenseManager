namespace Models;

/// <summary>
/// Справочник бизнес-статусов — человекочитаемые названия кодов статусов
/// </summary>
public class StatusDictionary
{
    /// <summary>PK</summary>
    public int Id { get; set; }

    /// <summary>Числовой код статуса (соответствует BusinessStatus enum)</summary>
    public int Code { get; set; }

    /// <summary>Название статуса (например, "Нет продукта")</summary>
    public string Name { get; set; } = null!;

    /// <summary>Описание статуса</summary>
    public string? Description { get; set; }

    /// <summary>Мягкое удаление</summary>
    public int ArchiveLevel { get; set; }
}
