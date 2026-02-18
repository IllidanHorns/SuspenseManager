namespace Models;

public class Territory
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Код территории (RU, US, UK)
    /// </summary>
    public string TerritoryCode { get; set; }
    
    /// <summary>
    /// Название территории (Россия, США, Великобритания)
    /// </summary>
    public string TerritoryName { get; set; }
    
    /// <summary>
    /// Мягкое удаление
    /// </summary>
    public int ArchiveLevel { get; set; }
    
    /// <summary>
    /// Время создания
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Время обновления
    /// </summary>
    public DateTime? ChangeTime { get; set; }
    
    /// <summary>
    /// Время архивации
    /// </summary>
    public DateTime? ArchiveTime { get; set; }
}