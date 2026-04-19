using Common.DTOs;

namespace Application.Interfaces;

public interface IAdminMetricsService
{
    Task<AdminMetricsDto> GetMetricsAsync(CancellationToken ct = default);
}
