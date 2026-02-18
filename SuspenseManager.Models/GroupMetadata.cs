namespace Models;

public class GroupMetadata
{
    public int Id  { get; set; }
    
    public int SuspenseGroupId {  get; set; }
    
    public SuspenseGroup SuspenseGroup { get; set; }
    
    public string? CatalogNumber { get; set; }
    
    public string? Barcode { get; set; }
    
    public string? Isrc { get; set; }
    
    public string? Artist { get; set; }
    
    public string? Title { get; set; }
    
    public string? Genre { get; set; }
    
    public string? Description { get; set; }
    
    public string? ProductTypeCode { get; set; }
    
    public string? ProductTypeDesc { get; set; }
    
    public int? Duration  { get; set; }
    
    public DateOnly? ReleaseDate { get; set; }
    
    public int? ProductTypeId { get; set; }
    
    public CatalogProductType? ProductType { get; set; }
    
    public int? CatalogProductId { get; set; }

    public CatalogProduct? CatalogProduct { get; set; }
    
    /// <summary>
    /// Время создания
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Время изменения
    /// </summary>
    public DateTime ChangeTime { get; set; }
    
    /// <summary>
    /// Время мягкого удаления
    /// </summary>
    public DateTime ArchiveTime { get; set; }
    
    /// <summary>
    /// Мягкое удаление
    /// </summary>
    public int ArchiveLevel { get; set; }
}