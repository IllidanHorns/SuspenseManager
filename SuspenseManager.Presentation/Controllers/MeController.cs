using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMeService _meService;
    private readonly IAccountService _accountService;

    public MeController(IMeService meService, IAccountService accountService)
    {
        _meService = meService;
        _accountService = accountService;
    }

    private int GetCurrentAccountId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("account_id");
        return int.TryParse(value, out var id) ? id : 0;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId <= 0)
        {
            return Unauthorized(ApiResponse<object>.Fail(401, "Не удалось определить аккаунт", "UNAUTHORIZED"));
        }

        var data = await _meService.GetSettingsAsync(accountId, ct);
        return Ok(ApiResponse<MeSettingsResponseDto>.Success(data));
    }

    /// <summary>Профиль текущего пользователя — доступен любому авторизованному аккаунту</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId <= 0)
            return Unauthorized(ApiResponse<object>.Fail(401, "Не удалось определить аккаунт", "UNAUTHORIZED"));

        var account = await _accountService.GetByIdAsync(accountId, ct);
        if (account == null)
            return NotFound(ApiResponse<object>.Fail(404, "Аккаунт не найден", "NOT_FOUND"));

        return Ok(ApiResponse<object>.Success(account));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateMeSettingsDto dto, CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId <= 0)
        {
            return Unauthorized(ApiResponse<object>.Fail(401, "Не удалось определить аккаунт", "UNAUTHORIZED"));
        }

        var data = await _meService.UpdateSettingsAsync(accountId, dto, ct);
        return Ok(ApiResponse<MeSettingsResponseDto>.Success(data, "Настройки сохранены", "ME_SETTINGS_UPDATED"));
    }
}
