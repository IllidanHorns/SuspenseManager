namespace Models;

public class Company
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Юридическое название
    /// </summary>
    public string LegalName { get; set; }

    /// <summary>
    /// Короткое название
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// Код компании
    /// </summary>
    public string CompanyCode { get; set; }

    /// <summary>
    /// Название банка
    /// </summary>
    public string BankName { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Страна
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Юридический адрес
    /// </summary>
    public string LegalAddress { get; set; }

    /// <summary>
    /// Фактический адрес
    /// </summary>
    public string ActualAddress { get; set; }

    /// <summary>
    /// ИНН
    /// </summary>
    public string Inn { get; set; }

    /// <summary>
    /// БИК
    /// </summary>
    public string Bic { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime? ChangeTime { get; set; }

    /// <summary>
    /// Уровень архивации
    /// </summary>
    public int ArchiveLevel { get; set; }

    /// <summary>
    /// Время архивации
    /// </summary>
    public DateTime? ArchiveTime { get; set; }
}
