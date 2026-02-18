namespace Models;

public class CatalogProduct
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Код формата продукта
    /// </summary>
    public string ProductFormatCode { get; set; }
    
    /// <summary>
    /// Полное описание формата продукта
    /// </summary>
    public string? ProductTypeDesc { get; set; }
    
    public int ProductTypeId { get; set; }
    
    public CatalogProductType ProductType { get; set; }
    
    /// <summary>
    /// Название продукта
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Баркод
    /// </summary>
    public string Barcode { get; set; }
    
    /// <summary>
    /// Название альбома
    /// </summary>
    public string? AlbumName { get; set; }
    
    /// <summary>
    /// Исполнитель трека
    /// </summary>
    public string? Artist { get; set; }
    
    /// <summary>
    /// Номер каталога
    /// </summary>
    public string CatalogNumber { get; set; }
    
    /// <summary>
    /// Композитор
    /// </summary>
    public string? Composer { get; set; }
    
    /// <summary>
    /// Описание продукта
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Дата релиза
    /// </summary>
    public DateOnly? ReleaseDate { get; set; }
    
    /// <summary>
    /// Isrc
    /// </summary>
    public string Isrc { get; set; }
    
    /// <summary>
    /// Жанр
    /// </summary>
    public string? Genre { get; set; }
    
    /// <summary>
    /// Дата изменния
    /// </summary>
    public DateTime? ChangeTime { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Дата архивации
    /// </summary>
    public DateTime? ArchiveTime { get; set; }

    /// <summary>
    /// Уровень архивации
    /// </summary>
    public int ArchiveLevel { get; set; }
    
    /// <summary>
    /// Коллекция прав продукта (1 продукт — много прав)
    /// </summary>
    public List<CatalogProductRights> ProductRights { get; set; }
}