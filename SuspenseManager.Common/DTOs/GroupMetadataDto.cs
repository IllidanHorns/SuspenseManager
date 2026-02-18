namespace Common.DTOs;

/// <summary>
/// DTO для обновления метаданных продукта группы (п.14)
/// </summary>
public class UpdateGroupMetadataDto
{
    public string? CatalogNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Isrc { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public string? ProductTypeCode { get; set; }
    public string? ProductTypeDesc { get; set; }
    public int? Duration { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int? ProductTypeId { get; set; }
    public int? CatalogProductId { get; set; }
}

/// <summary>
/// DTO для обновления метаданных прав группы (п.15)
/// </summary>
public class UpdateGroupMetaRightsDto
{
    public string? DocNumber { get; set; }
    public string? DocType { get; set; }
    public DateOnly? DocDate { get; set; }
    public DateOnly? DocStart { get; set; }
    public DateOnly? DocEnd { get; set; }
    public int? TerritoryId { get; set; }
    public string? TerritoryCode { get; set; }
    public string? TerritoryDesc { get; set; }
    public int? SenderCompanyId { get; set; }
    public int? ReceiverCompanyId { get; set; }
    public double? Share { get; set; }
}

/// <summary>
/// DTO для быстрой каталогизации (п.16)
/// </summary>
public class QuickCatalogDto
{
    public string Title { get; set; } = null!;
    public string Artist { get; set; } = null!;
    public string? Isrc { get; set; }
    public string? Barcode { get; set; }
    public string? CatalogNumber { get; set; }
    public string? ProductFormatCode { get; set; }
    public string? Genre { get; set; }
    public string? AlbumName { get; set; }
    public string? Composer { get; set; }
    public string? Description { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int? ProductTypeId { get; set; }
}

/// <summary>
/// DTO для отправки в бэк-офис (п.20)
/// </summary>
public class SendToBackOfficeDto
{
    public string? Comment { get; set; }
}

/// <summary>
/// DTO для откладывания группы (п.21)
/// </summary>
public class PostponeGroupDto
{
    public string? Reason { get; set; }
}

/// <summary>
/// DTO для привязки группы к продукту (п.25)
/// </summary>
public class LinkProductDto
{
    public int ProductId { get; set; }
}
