namespace Models;

public class CatalogProductType
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Сокращенный код формата продукта
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Описание формата продукта порлное
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Коллекция продуктов данного типа (1 тип — много продуктов)
    /// </summary>
    public List<CatalogProduct> CatalogProducts { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime? ChangeTime { get; set; }

    /// <summary>
    /// Уровень архивации
    /// </summary>
    public int ArchiveLevel { get; set; }

    /// <summary>
    /// Время архивации
    /// </summary>
    public DateTime? ArchiveTime { get; set; }
}
