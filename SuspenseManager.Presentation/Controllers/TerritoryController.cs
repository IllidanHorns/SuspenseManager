using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Models;

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

    /// <summary>Список территорий с пагинацией.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _territoryService.GetTerritoriesAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Territory>>.Success(result));
    }

    /// <summary>Создать территорию.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTerritoryDto dto, CancellationToken ct)
    {
        var territory = await _territoryService.CreateAsync(dto, ct);
        return StatusCode(201, ApiResponse<Territory>.Created(territory, "Территория создана", "TERRITORY_CREATED"));
    }

    /// <summary>Обновить территорию.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTerritoryDto dto, CancellationToken ct)
    {
        var territory = await _territoryService.UpdateAsync(id, dto, ct);
        return Ok(ApiResponse<Territory>.Success(territory, "Территория обновлена", "TERRITORY_UPDATED"));
    }
}
