namespace Common.DTOs;

public class CreateCatalogProductDto
{
    public string? ProductName { get; set; }
    public string? Artist { get; set; }
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

public class UpdateCatalogProductDto
{
    public string? ProductName { get; set; }
    public string? Artist { get; set; }
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

public class CreateCatalogProductRightsDto
{
    public int CatalogProductId { get; set; }
    public string? DocNumber { get; set; }
    public int CompanySenderId { get; set; }
    public int CompanyReceiverId { get; set; }
    public double Share { get; set; }
    public int TerritoryId { get; set; }
    public DateOnly DocStart { get; set; }
    public DateOnly DocEnd { get; set; }
}

public class UpdateCatalogProductRightsDto
{
    public string? DocNumber { get; set; }
    public int? CompanySenderId { get; set; }
    public int? CompanyReceiverId { get; set; }
    public double? Share { get; set; }
    public int? TerritoryId { get; set; }
    public DateOnly? DocStart { get; set; }
    public DateOnly? DocEnd { get; set; }
}

public class CreateCompanyDto
{
    public required string LegalName { get; set; }
    public required string ShortName { get; set; }
    public string? CompanyCode { get; set; }
    public string? BankName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? LegalAddress { get; set; }
    public string? ActualAddress { get; set; }
    public string? Inn { get; set; }
    public string? Bic { get; set; }
}

public class UpdateCompanyDto
{
    public string? LegalName { get; set; }
    public string? ShortName { get; set; }
    public string? CompanyCode { get; set; }
    public string? BankName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? LegalAddress { get; set; }
    public string? ActualAddress { get; set; }
    public string? Inn { get; set; }
    public string? Bic { get; set; }
}

public class CreateTerritoryDto
{
    public required string TerritoryCode { get; set; }
    public string? TerritoryName { get; set; }
}

public class UpdateTerritoryDto
{
    public string? TerritoryCode { get; set; }
    public string? TerritoryName { get; set; }
}
