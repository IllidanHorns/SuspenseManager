namespace Models;

public class GroupMetaRights
{
    public int Id { get; set; }
    
    public int GroupId { get; set; }
    
    public SuspenseGroup SuspenseGroup { get; set; }
    
    public string? DocNumber { get; set; }
    
    public string? DocType { get; set; }
    
    public DateOnly? DocDate { get; set; }
    
    public DateOnly? DocStart { get; set; }
    
    public DateOnly? DocEnd { get; set; }
    
    public Territory? Territory { get; set; }
    
    public int? TerritoryId { get; set; }
    
    public string? TerritoryDesc  { get; set; }
    
    public string? TerritoryCode { get; set; }
    
    public int? CatalogProductId { get; set; }
    
    public CatalogProduct? CatalogProduct { get; set; }
    
    /// <summary>
    /// FK компании отправителя
    /// </summary>
    public int? SenderCompanyId { get; set; }

    /// <summary>
    /// Сущность связи с компанией отправителем
    /// </summary>
    public Company? SenderCompany { get; set; }

    /// <summary>
    /// FK компании получателя
    /// </summary>
    public int? ReceiverCompanyId { get; set; }

    /// <summary>
    /// Сущность связи с компанией получателем
    /// </summary>
    public Company? ReceiverCompany { get; set; }

    public double? Share { get; set; }
    
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