namespace Models;

public class User
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Фамилия
    /// </summary>
    public string Surname { get; set; }

    /// <summary>
    /// Отчество
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Эл. почта
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    public string Position { get; set; }

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
    /// Сущность свзяи с аккаунтом
    /// </summary>
    public Account Account { get; set; }

}
