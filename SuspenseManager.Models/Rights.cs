namespace Models;

public class Rights
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Код права доступа (например "suspense.read", "groups.no_product.catalog_fast")
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Название права для отображения в UI
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Модуль к которому относится право (загрузка, группировка, администрирование и т.д.)
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Описание права
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Время создания
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Время изменения
    /// </summary>
    public DateTime? ChangeTime { get; set; }

    /// <summary>
    /// Время мягкого удаления
    /// </summary>
    public DateTime? ArchiveTime { get; set; }

    /// <summary>
    /// Мягкое удаление
    /// </summary>
    public int ArchiveLevel { get; set; }

    /// <summary>
    /// Коллекция связи many to many (аккаунт-права через таблицу промежуточную)
    /// </summary>
    public List<AccountRightsLink> AccountsLinks { get; set; }

    /// <summary>
    /// Коллекция связи many to many
    /// </summary>
    public List<Account> Accounts { get; set; }
}
