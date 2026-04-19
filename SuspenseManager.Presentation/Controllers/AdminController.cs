using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

/// <summary>Сводные метрики для экрана администрирования.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminMetricsService _metrics;

    public AdminController(IAdminMetricsService metrics)
    {
        _metrics = metrics;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var data = await _metrics.GetMetricsAsync(ct);
        return Ok(ApiResponse<AdminMetricsDto>.Success(data));
    }
}
