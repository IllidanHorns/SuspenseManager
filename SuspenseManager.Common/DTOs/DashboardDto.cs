namespace Common.DTOs;

public class DashboardDto
{
    public int TotalSuspenses { get; set; }
    public int NoProductCount { get; set; }
    public int NoRightsCount { get; set; }
    public int InGroupNoProduct { get; set; }
    public int InGroupNoRights { get; set; }
    public int ValidatedCount { get; set; }
    public int BackOfficeCount { get; set; }
    public int PostponedCount { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCompanies { get; set; }
    public decimal TotalRevenue { get; set; }
    public long TotalStreams { get; set; }
    public List<OperatorStatDto> TopOperators { get; set; } = [];
    public List<StatusCountDto> StatusDistribution { get; set; } = [];
}

public class OperatorStatDto
{
    public string Operator { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class StatusCountDto
{
    public int Status { get; set; }
    public int Count { get; set; }
}
