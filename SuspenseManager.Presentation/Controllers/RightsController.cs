using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

/// <summary>Справочник прав и готовые пресеты ролей для админки.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RightsController : ControllerBase
{
    private readonly IRightsCatalogService _rightsCatalog;

    public RightsController(IRightsCatalogService rightsCatalog)
    {
        _rightsCatalog = rightsCatalog;
    }

    /// <summary>Все активные права (id, код, название, модуль).</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.AdminPermissionsManage)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var items = await _rightsCatalog.GetCatalogAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<RightCatalogItemDto>>.Success(items));
    }

    /// <summary>Четыре дефолтных набора: полный доступ, админ, оператор, бэк-офис.</summary>
    [HttpGet("presets")]
    [RequirePermission(PermissionCodes.AdminPermissionsManage)]
    public async Task<IActionResult> GetPresets(CancellationToken ct)
    {
        var items = await _rightsCatalog.GetRolePresetsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<RolePresetDto>>.Success(items));
    }
}
