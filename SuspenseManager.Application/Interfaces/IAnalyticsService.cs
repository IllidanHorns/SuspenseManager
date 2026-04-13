using Common.DTOs;

namespace Application.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
}
