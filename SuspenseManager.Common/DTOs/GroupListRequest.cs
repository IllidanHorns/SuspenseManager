namespace Common.DTOs;

public class GroupListRequest : PagedRequest
{
    /// <summary>
    /// Только группы, созданные текущим аккаунтом (по AccountId группы).
    /// </summary>
    public bool OnlyMine { get; set; }

    public string? Artist { get; set; }
    public string? Isrc { get; set; }
    public string? Barcode { get; set; }
    public string? Title { get; set; }
    public string? TerritoryCode { get; set; }
    public string? DocNumber { get; set; }
    public int? CountMin { get; set; }
    public int? CountMax { get; set; }
    public decimal? RevenueMin { get; set; }
    public decimal? RevenueMax { get; set; }
}
