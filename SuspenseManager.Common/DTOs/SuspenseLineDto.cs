namespace Common.DTOs;

/// <summary>
/// DTO для входящей строки суспенса (из Excel-отчёта или формы ручного ввода)
/// </summary>
public class SuspenseLineDto
{
    /// <summary>
    /// Идентификатор фонограммы (ISRC)
    /// </summary>
    public string? Isrc { get; set; }

    /// <summary>
    /// Баркод продукта
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Каталожный номер
    /// </summary>
    public string? CatalogNumber { get; set; }

    /// <summary>
    /// Код формата продукта (TTkey)
    /// </summary>
    public string? ProductFormatCode { get; set; }

    /// <summary>
    /// Компания отправитель (название из отчёта)
    /// </summary>
    public string? SenderCompany { get; set; }

    /// <summary>
    /// Компания получатель (название из отчёта)
    /// </summary>
    public string? RecipientCompany { get; set; }

    /// <summary>
    /// Оператор (стриминговая площадка)
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// Артист
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Название трека
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
    /// Кол-во прослушиваний
    /// </summary>
    public int Qty { get; set; }

    /// <summary>
    /// Цена за одно прослушивание
    /// </summary>
    public double? Ppd { get; set; }

    /// <summary>
    /// Валюта
    /// </summary>
    public decimal ExchangeCurrency { get; set; }

    /// <summary>
    /// Курс обмена
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// Жанр
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// FK компании отправителя (если известен)
    /// </summary>
    public int? SenderCompanyId { get; set; }

    /// <summary>
    /// FK компании получателя (если известен)
    /// </summary>
    public int? RecipientCompanyId { get; set; }
}
