namespace Models;

public class Account
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
    /// Логин 
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// Хеш пароля
    /// </summary>
    public string PasswordHash { get; set; }
    
    /// <summary>
    /// Описание для аккаунта
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// FK для таблицы пользовательских данных
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Модель связи с таблицей  пользовательски данных
    /// </summary>
    public User? User { get; set; }
    
    /// <summary>
    /// Коллекция связи many to many (аккаунт-права через таблицу промежуточную)
    /// </summary>
    public List<AccountRightsLink>  RightsLinks { get; set; }
    
    /// <summary>
    /// Коллекция связи many to many 
    /// </summary>
    public List<Rights> Rights { get; set; }
    
}