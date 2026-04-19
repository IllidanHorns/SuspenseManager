using Common.DTOs;

namespace Application.Interfaces;

public interface IMeService
{
    Task<MeSettingsResponseDto> GetSettingsAsync(int accountId, CancellationToken ct = default);
    Task<MeSettingsResponseDto> UpdateSettingsAsync(int accountId, UpdateMeSettingsDto dto, CancellationToken ct = default);
}
