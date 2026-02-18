namespace Models;

public class AccountRightsLink
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }
    
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
    
    /// <summary>
    /// FK для связи с таблицей прав
    /// </summary>
    public int RightId { get; set; }
    
    /// <summary>
    /// FK для связи с таблицей аккаунтов
    /// </summary>
    public int AccountId { get; set; }
    
    /// <summary>
    /// Сущность связи с правами (данная сущность таблица - many to many)
    /// </summary>
    public Rights Rights { get; set; }
    
    /// <summary>
    /// Сущность связи с аккауном (данная сущность таблица - many to many)
    /// </summary>
    public Account Account { get; set; }
}