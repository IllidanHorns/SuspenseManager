namespace Models;

public class CatalogProductRights
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Номер договора
    /// </summary>
    public string? DocNumber { get; set; }

    /// <summary>
    /// Компания отправитель
    /// </summary>
    public string CompanySender { get; set; }

    /// <summary>
    /// Коампания получатель
    /// </summary>
    public string CompanyReceiver { get; set; }

    /// <summary>
    /// FK Комапнии отправителя
    /// </summary>
    public int CompanySenderId { get; set; }

    /// <summary>
    /// FK компании получаетля
    /// </summary>
    public int CompanyReceiverId { get; set; }

    /// <summary>
    /// Сущность модели  компании отправителя
    /// </summary>
    public Company CompanySenderR { get; set; }

    /// <summary>
    /// Сущность модели компании получаетелдя
    /// </summary>
    public Company CompanyReceiverR { get; set; }

    /// <summary>
    /// Доля на продукт компании
    /// </summary>
    public double Share { get; set; }

    /// <summary>
    /// Код территории
    /// </summary>
    public string TerritoryCode { get; set; }

    /// <summary>
    /// Описание территории
    /// </summary>
    public string TerritoryDesc { get; set; }

    /// <summary>
    /// FK териитории
    /// </summary>
    public int TerritoryId { get; set; }

    /// <summary>
    /// Сущность моедли связи для территории
    /// </summary>
    public Territory Territory { get; set; }

    /// <summary>
    /// FK продукта каталога
    /// </summary>
    public int CatalogProductId { get; set; }

    /// <summary>
    /// Сущность связи с продуктом каталога
    /// </summary>
    public CatalogProduct CatalogProduct { get; set; }

    /// <summary>
    /// Дата начала действия договора
    /// </summary>
    public DateOnly DocStart { get; set; }

    /// <summary>
    /// Дата конца действия договора
    /// </summary>
    public DateOnly DocEnd { get; set; }

    /// <summary>
    /// Время создания
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Время изменения
    /// </summary>
    public DateTime? ChangeTime { get; set; }

    /// <summary>
    /// Время архивации
    /// </summary>
    public DateTime? ArchiveTime { get; set; }

    /// <summary>
    /// Мягкое удаление
    /// </summary>
    public int ArchiveLevel { get; set; }
}
