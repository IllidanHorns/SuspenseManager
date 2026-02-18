namespace Models;

public class SuspenseGroup
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Основной статус, с помощью которого определяется состояние объекта.
    /// Статус является общим определением дял строки суспенса и группы, и талицы метаданных
    /// </summary>
    public int BusinessStatus { get; set; }

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

    /// <summary>
    /// FK для связи с аккантом
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Сущность связи аккаунта
    /// </summary>
    public Account Account { get; set; }

    /// <summary>
    /// FK для связи с продуктом каталога (заполняется при привязке к продукту)
    /// </summary>
    public int? CatalogProductId { get; set; }

    /// <summary>
    /// Сущность связи с продуктом каталога
    /// </summary>
    public CatalogProduct? CatalogProduct { get; set; }

    public int? MetaDataId { get; set; }

    public GroupMetadata? GroupMetaData { get; set; }

    public int? MetaRightsId { get; set; }

    public GroupMetaRights? GroupMetaRights { get; set; }

    /// <summary>
    /// Коллекция суспенсов в группе
    /// </summary>
    public List<SuspenseLine> SuspenseLines { get; set; }

    /// <summary>
    /// Коллекция связей суспенсов с группой (для истории)
    /// </summary>
    public List<SuspenseGroupLink> SuspenseGroupLinks { get; set; }
}
