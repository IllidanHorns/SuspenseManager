using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface ITerritoryService
{
    Task<PagedResponse<Territory>> GetTerritoriesAsync(PagedRequest request, CancellationToken ct = default);
}
