using Common.DTOs;

namespace Application.Interfaces;

public interface IRightsCatalogService
{
    Task<IReadOnlyList<RightCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RolePresetDto>> GetRolePresetsAsync(CancellationToken ct = default);
}
