using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер территорий — п.13
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TerritoryController : ControllerBase
{
    private readonly ITerritoryService _territoryService;

    public TerritoryController(ITerritoryService territoryService)
    {
        _territoryService = territoryService;
    }

    /// <summary>
    /// Список территорий с пагинацией
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _territoryService.GetTerritoriesAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.Territory>>.Success(result));
    }
}
