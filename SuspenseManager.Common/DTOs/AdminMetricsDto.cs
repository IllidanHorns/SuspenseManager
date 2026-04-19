namespace Common.DTOs;

/// <summary>Сводные метрики для экрана администрирования.</summary>
public class AdminMetricsDto
{
    public int UsersTotal { get; set; }
    public int AccountsTotal { get; set; }
    public int AccountsWithoutUserProfile { get; set; }
    public int UsersWithoutAccount { get; set; }
    public int RightsTotal { get; set; }
}
