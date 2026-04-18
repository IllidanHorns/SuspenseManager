using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface ITerritoryService
{
    Task<PagedResponse<Territory>> GetTerritoriesAsync(PagedRequest request, CancellationToken ct = default);
    Task<Territory> CreateAsync(CreateTerritoryDto dto, CancellationToken ct = default);
    Task<Territory> UpdateAsync(int id, UpdateTerritoryDto dto, CancellationToken ct = default);
}
