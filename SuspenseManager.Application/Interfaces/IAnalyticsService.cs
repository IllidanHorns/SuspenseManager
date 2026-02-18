namespace Application.Interfaces;

public interface IAnalyticsService
{
    Task<object> GetDashboardAsync(CancellationToken ct = default);
}
