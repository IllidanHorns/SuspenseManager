using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Авторизация по логину/паролю. Возвращает access + refresh токены.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        _logger.LogInformation("Вход в систему: AccountId={AccountId}, Login={Login}", result.AccountId, result.Login);
        return Ok(ApiResponse<TokenResponseDto>.Success(result, "Авторизация успешна", "LOGIN_SUCCESS"));
    }

    /// <summary>
    /// Обновление access токена по refresh токену. Возвращает новую пару токенов (rotation).
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(dto.RefreshToken, ct);
        return Ok(ApiResponse<TokenResponseDto>.Success(result, "Токен обновлён", "TOKEN_REFRESHED"));
    }

    /// <summary>
    /// Отзыв refresh токена (logout).
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        await _authService.RevokeAsync(dto.RefreshToken, ct);
        return Ok(ApiResponse<object>.Success(null!, "Токен отозван", "TOKEN_REVOKED"));
    }
}
