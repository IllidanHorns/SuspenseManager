namespace Models;

public class SuspenseGroupLink
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// FK Линии суспенса
    /// </summary>
    public int SuspenseId { get; set; }

    /// <summary>
    /// FK группы суспенсов
    /// </summary>
    public int SuspenseGroupId { get; set; }

    /// <summary>
    /// FK Акканта пользователя
    /// </summary>
    public int AccountId { get; set; }

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
    /// Сущность связи акканута
    /// </summary>
    public Account Account { get; set; }

    /// <summary>
    /// Сущность связи с группой суспенсов
    /// </summary>
    public SuspenseGroup SuspenseGroup { get; set; }

    /// <summary>
    /// Основной статус, с помощью которого определяется состояние объекта.
    /// Статус является общим определением дял строки суспенса и группы, и талицы метаданных
    /// </summary>
    public int BusinessStatus { get; set; }

    /// <summary>
    /// Сущность связи с строкой суспенса
    /// </summary>
    public SuspenseLine SuspenseLine { get; set; }
}
