namespace Models;

public class SuspenseLine
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Идентификатор фонограммы
    /// </summary>
    public string? Isrc { get; set; }
    
    /// <summary>
    /// Баркод - идентификатор продукта
    /// </summary>
    public string? Barcode { get; set; }
    
    /// <summary>
    /// Каталожный номер
    /// </summary>
    public string? CatalogNumber { get; set; }
    
    /// <summary>
    /// Идентификатор продукта
    /// </summary>
    public int? ProductId { get; set; }
    
    /// <summary>
    /// Компания отправитель
    /// </summary>
    public string? SenderCompany { get; set; }
    
    /// <summary>
    /// Компания получаетель
    /// </summary>
    public string? RecipientCompany { get; set; }
    
    /// <summary>
    /// Стриминеговая компания
    /// </summary>
    public string? Operator { get; set; }
    
    /// <summary>
    /// Артист
    /// </summary>
    public string? Artist { get; set; }
    
    /// <summary>
    /// Названия фонограмма
    /// </summary>
    public string? TrackTitle { get; set; }
    
    /// <summary>
    /// Тип договора
    /// </summary>
    public string? AgreementType { get; set; }
    
    /// <summary>
    /// Номер договора
    /// </summary>
    public string? AgreementNumber { get; set; }
    
    /// <summary>
    /// Код территории
    /// </summary>
    public string? TerritoryCode { get; set; }
    
    /// <summary>
    /// Причина попадания в суспенс
    /// </summary>
    public string CauseSuspense { get; set; }
    
    /// <summary>
    /// Кол-во прослушиваний в стриме
    /// </summary>
    public int Qty { get; set; }
    
    /// <summary>
    /// Сумма денег за 1 прослушивание
    /// </summary>
    public double? Ppd { get; set; }
    
    /// <summary>
    /// Валюта 
    /// </summary>
    public decimal ExchangeCurrency { get; set; }
    
    /// <summary>
    /// Курс обмена в рубли
    /// </summary>
    public decimal ExchangeRate { get; set; }
    
    /// <summary>
    /// Время изменения сущности 
    /// </summary>
    public DateTime? ChangeTime { get; set; }
    
    /// <summary>
    /// Время создания сущности
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Основной статус, с помощью которого определяется состояние объекта.
    /// Статус является общим определением дял строки суспенса и группы, и талицы метаданных
    /// </summary>
    public int BusinessStatus { get; set; }
    
    /// <summary>
    /// Мягкое удаление
    /// </summary>
    public int ArchiveLevel { get; set; }
    
    /// <summary>
    /// Время мягкого удаления
    /// </summary>
    public DateTime? ArchiveTime { get; set; }
    
    /// <summary>
    /// Жанр фонограммы
    /// </summary>
    public string? Genre { get; set; }
    
    /// <summary>
    /// FK текущей группы суспенса (nullable — суспенс может быть без группы)
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Сущность связи с текущей группой
    /// </summary>
    public SuspenseGroup? Group { get; set; }

    /// <summary>
    /// Сущность связи с продуктом каталога
    /// </summary>
    public CatalogProduct? CatalogProduct { get; set; }

    /// <summary>
    /// Компания отправитель
    /// </summary>
    public int? SenderCompanyId { get; set; }
    
    /// <summary>
    /// Компания получатель
    /// </summary>
    public int? RecipientCompanyId { get; set; }
    
    /// <summary>
    /// Сущность для компании отправителя
    /// </summary>
    public Company? SenderCompanyR { get; set; }
    
    /// <summary>
    /// Сущность для компании получаетеля
    /// </summary>
    public Company? RecipientCompanyR { get; set; }
}