using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface ISuspenseService
{
    Task<PagedResponse<SuspenseLine>> GetSuspensesAsync(PagedRequest request, CancellationToken ct = default);
    Task<SuspenseLine?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SuspenseLine> UpdateAsync(int id, SuspenseLineDto dto, CancellationToken ct = default);

    /// <summary>
    /// Несгруппированные суспенсы по статусу (0 = нет продукта, 1 = нет прав)
    /// </summary>
    Task<PagedResponse<SuspenseLine>> GetUngroupedAsync(int businessStatus, PagedRequest request, CancellationToken ct = default);
}
