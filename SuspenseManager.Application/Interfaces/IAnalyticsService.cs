using Common.DTOs;

namespace Application.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
}
